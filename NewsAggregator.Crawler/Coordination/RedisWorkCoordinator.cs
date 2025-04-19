using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Services;
using NewsAggregator.Domain.News.ValueObjects;
using StackExchange.Redis;

namespace NewsAggregator.Crawler.Coordination
{
    /// <summary>
    /// Redis-based implementation of the work coordinator
    /// </summary>
    public class RedisWorkCoordinator : IWorkCoordinator
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISourceService _sourceService;
        private readonly ILogger<RedisWorkCoordinator> _logger;
        private readonly DistributedCrawlerOptions _options;
        
        private const string PendingSourcesKey = "crawler:sources:pending";
        private const string ProcessingSourcesKey = "crawler:sources:processing";
        private const string CompletedSourcesKey = "crawler:sources:completed";
        private const string WorkerStatsKey = "crawler:workers";
        private const string LastResetKey = "crawler:last_reset";
        
        public RedisWorkCoordinator(
            IConnectionMultiplexer redis,
            ISourceService sourceService,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<RedisWorkCoordinator> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }
        
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            
            // Check if we need to initialize
            if (await db.SetLengthAsync(PendingSourcesKey) > 0 || 
                await db.SetLengthAsync(ProcessingSourcesKey) > 0 ||
                await db.SetLengthAsync(CompletedSourcesKey) > 0)
            {
                _logger.LogInformation("Work coordination state exists, no initialization needed");
                return;
            }
            
            // Fetch all news sources
            var sources = await _sourceService.GetAllSourcesAsync(cancellationToken);
            
            if (!sources.Any())
            {
                _logger.LogWarning("No sources available for crawling");
                return;
            }
            
            // Add all sources to the pending set
            var batch = db.CreateBatch();
            foreach (var source in sources)
            {
                string sourceJson = JsonSerializer.Serialize(source);
                batch.SetAddAsync(PendingSourcesKey, sourceJson);
            }
            
            await batch.ExecuteAsync();
            await db.StringSetAsync(LastResetKey, DateTime.UtcNow.ToString("o"));
            
            _logger.LogInformation("Initialized work coordination with {Count} sources", sources.Count());
        }
        
        public async Task<IEnumerable<NewsSource>> AcquireSourcesAsync(int batchSize, string workerId, CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            var acquiredSources = new List<NewsSource>();
            
            for (int i = 0; i < batchSize; i++)
            {
                var sourceJson = await db.SetPopAsync(PendingSourcesKey);
                if (sourceJson.IsNullOrEmpty)
                {
                    break; // No more pending sources
                }
                
                try
                {
                    var source = JsonSerializer.Deserialize<NewsSource>(sourceJson);
                    acquiredSources.Add(source);
                    
                    // Add to processing set with worker info
                    var processingInfo = new
                    {
                        Source = source,
                        WorkerId = workerId,
                        StartTime = DateTime.UtcNow
                    };
                    
                    await db.SetAddAsync(ProcessingSourcesKey, JsonSerializer.Serialize(processingInfo));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing source: {SourceJson}", sourceJson);
                }
            }
            
            _logger.LogInformation("Worker {WorkerId} acquired {Count} sources", workerId, acquiredSources.Count);
            return acquiredSources;
        }
        
        public async Task ReportSourceCompletionAsync(NewsSource source, int articlesCount, string workerId, CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            
            // Find and remove from processing set
            var processingEntries = await db.SetMembersAsync(ProcessingSourcesKey);
            foreach (var entry in processingEntries)
            {
                try
                {
                    var processingInfo = JsonSerializer.Deserialize<dynamic>(entry);
                    var sourceFromRedis = processingInfo.GetProperty("Source");
                    
                    if (sourceFromRedis.GetProperty("Url").GetString() == source.Url.ToString())
                    {
                        await db.SetRemoveAsync(ProcessingSourcesKey, entry);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing completion for source: {Url}", source.Url);
                }
            }
            
            // Add to completed set with results
            var completionInfo = new
            {
                Source = source,
                WorkerId = workerId,
                ArticlesCount = articlesCount,
                CompletionTime = DateTime.UtcNow
            };
            
            await db.SetAddAsync(CompletedSourcesKey, JsonSerializer.Serialize(completionInfo));
            
            // Update worker stats
            var workerKey = $"{WorkerStatsKey}:{workerId}";
            var stats = await db.HashGetAllAsync(workerKey);
            var sourcesProcessed = stats.FirstOrDefault(s => s.Name == "SourcesProcessed").Value;
            var articlesProcessedTotal = stats.FirstOrDefault(s => s.Name == "ArticlesProcessed").Value;
            
            await db.HashSetAsync(workerKey, new[]
            {
                new HashEntry("SourcesProcessed", sourcesProcessed.IsNull ? 1 : (int)sourcesProcessed + 1),
                new HashEntry("ArticlesProcessed", articlesProcessedTotal.IsNull ? articlesCount : (int)articlesProcessedTotal + articlesCount),
                new HashEntry("LastActivity", DateTime.UtcNow.ToString("o"))
            });
            
            _logger.LogInformation("Worker {WorkerId} completed source {Url} with {ArticlesCount} articles", 
                workerId, source.Url, articlesCount);
        }
        
        public async Task<bool> IsWorkCompleteAsync(CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            
            long pendingCount = await db.SetLengthAsync(PendingSourcesKey);
            long processingCount = await db.SetLengthAsync(ProcessingSourcesKey);
            
            return pendingCount == 0 && processingCount == 0;
        }
        
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            
            // Save stats for this run
            var completedStats = await db.SetMembersAsync(CompletedSourcesKey);
            var runId = Guid.NewGuid().ToString();
            var statsKey = $"crawler:runs:{runId}";
            
            await db.HashSetAsync(statsKey, new HashEntry[]
            {
                new HashEntry("RunId", runId),
                new HashEntry("CompletedSources", completedStats.Length),
                new HashEntry("CompletionTime", DateTime.UtcNow.ToString("o")),
                new HashEntry("StartTime", await db.StringGetAsync(LastResetKey))
            });
            
            // Clear all sets
            await db.KeyDeleteAsync(new RedisKey[] { PendingSourcesKey, ProcessingSourcesKey, CompletedSourcesKey });
            
            // Set new reset time
            await db.StringSetAsync(LastResetKey, DateTime.UtcNow.ToString("o"));
            
            _logger.LogInformation("Reset work coordination. Previous run completed {Count} sources", completedStats.Length);
            
            // Re-initialize
            await InitializeAsync(cancellationToken);
        }
    }
} 