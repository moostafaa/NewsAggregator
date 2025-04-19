using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Crawler.Coordination
{
    /// <summary>
    /// Local in-memory implementation of work coordinator for development or single-server mode
    /// </summary>
    public class LocalWorkCoordinator : IWorkCoordinator
    {
        private readonly ISourceService _sourceService;
        private readonly ILogger<LocalWorkCoordinator> _logger;
        private readonly DistributedCrawlerOptions _options;
        
        private ConcurrentQueue<NewsSource> _pendingSources;
        private ConcurrentDictionary<string, (NewsSource Source, string WorkerId, DateTime StartTime)> _processingSources;
        private ConcurrentBag<(NewsSource Source, string WorkerId, int ArticlesCount, DateTime CompletionTime)> _completedSources;
        private DateTime _lastReset;
        
        public LocalWorkCoordinator(
            ISourceService sourceService,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<LocalWorkCoordinator> logger)
        {
            _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _pendingSources = new ConcurrentQueue<NewsSource>();
            _processingSources = new ConcurrentDictionary<string, (NewsSource, string, DateTime)>();
            _completedSources = new ConcurrentBag<(NewsSource, string, int, DateTime)>();
            _lastReset = DateTime.MinValue;
        }
        
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Only initialize if we haven't already
            if (_pendingSources.Count > 0 || _processingSources.Count > 0 || _completedSources.Count > 0)
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
            
            // Add all sources to the pending queue
            _pendingSources = new ConcurrentQueue<NewsSource>(sources);
            _lastReset = DateTime.UtcNow;
            
            _logger.LogInformation("Initialized work coordination with {Count} sources", _pendingSources.Count);
        }
        
        public Task<IEnumerable<NewsSource>> AcquireSourcesAsync(int batchSize, string workerId, CancellationToken cancellationToken = default)
        {
            var acquiredSources = new List<NewsSource>();
            
            for (int i = 0; i < batchSize; i++)
            {
                if (_pendingSources.TryDequeue(out var source))
                {
                    acquiredSources.Add(source);
                    _processingSources[source.Url.ToString()] = (source, workerId, DateTime.UtcNow);
                }
                else
                {
                    break; // No more pending sources
                }
            }
            
            _logger.LogInformation("Worker {WorkerId} acquired {Count} sources", workerId, acquiredSources.Count);
            return Task.FromResult<IEnumerable<NewsSource>>(acquiredSources);
        }
        
        public Task ReportSourceCompletionAsync(NewsSource source, int articlesCount, string workerId, CancellationToken cancellationToken = default)
        {
            if (_processingSources.TryRemove(source.Url.ToString(), out _))
            {
                _completedSources.Add((source, workerId, articlesCount, DateTime.UtcNow));
                
                _logger.LogInformation("Worker {WorkerId} completed source {Url} with {ArticlesCount} articles", 
                    workerId, source.Url, articlesCount);
            }
            else
            {
                _logger.LogWarning("Worker {WorkerId} reported completion for source {Url} but it wasn't in processing state", 
                    workerId, source.Url);
            }
            
            return Task.CompletedTask;
        }
        
        public Task<bool> IsWorkCompleteAsync(CancellationToken cancellationToken = default)
        {
            var isComplete = _pendingSources.IsEmpty && _processingSources.IsEmpty;
            return Task.FromResult(isComplete);
        }
        
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            // Log stats
            var sourcesCompleted = _completedSources.Count;
            var articlesProcessed = _completedSources.Sum(s => s.ArticlesCount);
            var timeTaken = DateTime.UtcNow - _lastReset;
            
            _logger.LogInformation(
                "Reset work coordination. Previous run completed {SourceCount} sources with {ArticleCount} articles in {TimeTaken:hh\\:mm\\:ss}",
                sourcesCompleted, articlesProcessed, timeTaken);
            
            // Clear all collections
            _pendingSources = new ConcurrentQueue<NewsSource>();
            _processingSources.Clear();
            _completedSources = new ConcurrentBag<(NewsSource, string, int, DateTime)>();
            
            // Re-initialize
            await InitializeAsync(cancellationToken);
        }
    }
} 