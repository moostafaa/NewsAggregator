using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.BackgroundServices
{
    public class NewsCrawlerBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NewsCrawlerBackgroundService> _logger;
        private readonly CrawlerOptions _options;

        public NewsCrawlerBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<CrawlerOptions> options,
            ILogger<NewsCrawlerBackgroundService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("News crawler background service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running news crawler job");

                try
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    
                    // Get crawler service from DI
                    var crawlerService = scope.ServiceProvider.GetRequiredService<INewsCrawlerService>();
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryClassificationService>();
                    var validCategories = await categoryService.GetValidCategoriesAsync();
                    
                    _logger.LogInformation("Valid categories in the system: {Categories}", string.Join(", ", validCategories));
                    
                    // Fetch articles from all sources
                    var articles = await crawlerService.FetchArticlesFromAllSourcesAsync(
                        _options.MaxArticlesPerSource, 
                        stoppingToken);
                    
                    // Get article repository from DI
                    var articleRepository = scope.ServiceProvider.GetRequiredService<INewsArticleRepository>();
                    
                    // Create a batch counter for logging progress
                    int totalArticles = articles.Count();
                    int savedArticles = 0;
                    int errorCount = 0;
                    
                    _logger.LogInformation("Preparing to save {Count} articles", totalArticles);
                    
                    // Use a concurrent queue to process articles in batches
                    var articlesQueue = new ConcurrentQueue<NewsArticle>(articles);
                    
                    // Configure the batch size and parallelism
                    int batchSize = 20;
                    int maxConcurrentBatches = Math.Max(1, _options.MaxConcurrentSources / 2);
                    
                    // Process articles in parallel batches
                    using var throttler = new SemaphoreSlim(maxConcurrentBatches);
                    var tasks = new List<Task>();
                    
                    while (!articlesQueue.IsEmpty)
                    {
                        // Create a batch of articles to process
                        var batch = new List<NewsArticle>();
                        for (int i = 0; i < batchSize; i++)
                        {
                            if (articlesQueue.TryDequeue(out var article))
                            {
                                batch.Add(article);
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        if (batch.Count == 0)
                            break;
                            
                        // Wait for a slot to process this batch
                        await throttler.WaitAsync(stoppingToken);
                        
                        // Process this batch in parallel
                        tasks.Add(Task.Run(async () => 
                        {
                            try
                            {
                                foreach (var article in batch)
                                {
                                    try
                                    {
                                        await articleRepository.AddAsync(article, stoppingToken);
                                        Interlocked.Increment(ref savedArticles);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error saving article: {Title}", article.Title);
                                        Interlocked.Increment(ref errorCount);
                                    }
                                }
                                
                                _logger.LogInformation("Saved batch of {Count} articles. Progress: {Saved}/{Total}", 
                                    batch.Count, savedArticles, totalArticles);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }, stoppingToken));
                    }
                    
                    // Wait for all batches to complete
                    await Task.WhenAll(tasks);
                    
                    _logger.LogInformation("Completed news crawler job. Saved {Saved} articles, {ErrorCount} errors",
                        savedArticles, errorCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running news crawler job");
                }

                // Wait for the next interval
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("News crawler background service is stopping");
        }
    }
} 