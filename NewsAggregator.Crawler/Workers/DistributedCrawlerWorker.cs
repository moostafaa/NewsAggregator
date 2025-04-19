using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Coordination;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Services;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Crawler.Workers
{
    public class DistributedCrawlerWorker : BackgroundService
    {
        private readonly IWorkCoordinator _coordinator;
        private readonly IArticlePublisher _publisher;
        private readonly ICategoryClassificationService _categoryClassifier;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DistributedCrawlerWorker> _logger;
        private readonly DistributedCrawlerOptions _options;
        private readonly string _workerId;
        
        private int _sourcesProcessed;
        private int _articlesPublished;
        
        public DistributedCrawlerWorker(
            IWorkCoordinator coordinator,
            IArticlePublisher publisher,
            ICategoryClassificationService categoryClassifier,
            HttpClient httpClient,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<DistributedCrawlerWorker> logger)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _categoryClassifier = categoryClassifier ?? throw new ArgumentNullException(nameof(categoryClassifier));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _workerId = _options.ServerName;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Distributed crawler worker starting. Worker ID: {WorkerId}", _workerId);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Initialize the work coordinator
                    await _coordinator.InitializeAsync(stoppingToken);
                    
                    // Reset counters
                    _sourcesProcessed = 0;
                    _articlesPublished = 0;
                    
                    // Process sources in parallel
                    await ProcessSourcesAsync(stoppingToken);
                    
                    // Report stats
                    await _publisher.ReportCrawlerStatsAsync(
                        _workerId, 
                        _sourcesProcessed, 
                        _articlesPublished, 
                        stoppingToken);
                    
                    _logger.LogInformation("Crawler run complete. Processed {SourceCount} sources, published {ArticleCount} articles",
                        _sourcesProcessed, _articlesPublished);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in crawler worker execution");
                }
                
                // Wait for next interval
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            }
        }
        
        private async Task ProcessSourcesAsync(CancellationToken cancellationToken)
        {
            // Create worker tasks
            var workerTasks = new List<Task>();
            var workerSemaphore = new SemaphoreSlim(_options.WorkerThreads);
            
            bool hasMoreWork = true;
            
            while (hasMoreWork && !cancellationToken.IsCancellationRequested)
            {
                // Acquire a batch of sources
                var sources = await _coordinator.AcquireSourcesAsync(_options.BatchSize, _workerId, cancellationToken);
                
                if (!sources.Any())
                {
                    // Check if we need to wait for other work to complete
                    var isComplete = await _coordinator.IsWorkCompleteAsync(cancellationToken);
                    if (isComplete)
                    {
                        hasMoreWork = false;
                        _logger.LogInformation("No more sources to process");
                        break;
                    }
                    
                    // Wait a bit and try again
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }
                
                // Process each source in the batch
                foreach (var source in sources)
                {
                    await workerSemaphore.WaitAsync(cancellationToken);
                    
                    workerTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessSourceAsync(source, cancellationToken);
                        }
                        finally
                        {
                            workerSemaphore.Release();
                        }
                    }, cancellationToken));
                }
            }
            
            // Wait for all workers to finish
            await Task.WhenAll(workerTasks);
        }
        
        private async Task ProcessSourceAsync(NewsSource source, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing source: {SourceName}", source.Name);
                
                // Fetch articles from the source
                var articles = await FetchArticlesFromSourceAsync(source, cancellationToken);
                
                // Publish articles
                var publishedCount = await _publisher.PublishArticlesAsync(articles, cancellationToken);
                
                // Report completion to the coordinator
                await _coordinator.ReportSourceCompletionAsync(source, articles.Count, _workerId, cancellationToken);
                
                // Update counters
                Interlocked.Increment(ref _sourcesProcessed);
                Interlocked.Add(ref _articlesPublished, publishedCount);
                
                _logger.LogInformation("Completed source {SourceName}. Published {PublishedCount} of {TotalCount} articles",
                    source.Name, publishedCount, articles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source: {SourceName}", source.Name);
                
                // Report failure
                await _coordinator.ReportSourceCompletionAsync(source, 0, _workerId, cancellationToken);
            }
        }
        
        private async Task<List<NewsArticle>> FetchArticlesFromSourceAsync(
            NewsSource source,
            CancellationToken cancellationToken)
        {
            var articles = new List<NewsArticle>();
            
            try
            {
                // Fetch the RSS feed
                var response = await _httpClient.GetStringAsync(source.Url, cancellationToken);
                var feed = System.Xml.Linq.XDocument.Parse(response);
                
                // Use XML namespaces to support different RSS formats
                var ns = feed.Root.GetDefaultNamespace();
                
                // Extract the channel element
                var channel = feed.Descendants(ns + "channel").FirstOrDefault();
                if (channel == null)
                {
                    _logger.LogWarning("Unable to find channel element in RSS feed for source: {SourceName}", source.Name);
                    return articles;
                }
                
                // Extract the items from the feed
                var items = channel.Elements(ns + "item").Take(_options.MaxArticlesPerSource);
                if (!items.Any())
                {
                    _logger.LogWarning("No items found in RSS feed for source: {SourceName}", source.Name);
                    return articles;
                }
                
                foreach (var item in items)
                {
                    try
                    {
                        // Extract basic information from the RSS item
                        var title = item.Element(ns + "title")?.Value.Trim() ?? string.Empty;
                        var link = item.Element(ns + "link")?.Value.Trim() ?? string.Empty;
                        var description = item.Element(ns + "description")?.Value.Trim() ?? string.Empty;
                        var pubDateStr = item.Element(ns + "pubDate")?.Value.Trim() ?? string.Empty;
                        var sourceCategory = item.Elements(ns + "category").FirstOrDefault()?.Value.Trim() ?? string.Empty;
                        
                        // Skip if missing essential info
                        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                        {
                            continue;
                        }
                        
                        // Parse the publication date
                        if (!DateTime.TryParse(pubDateStr, out var pubDate))
                        {
                            pubDate = DateTime.UtcNow;
                        }
                        
                        // Clean the HTML from description
                        string cleanDescription = CleanHtml(description);
                        
                        // Fetch full content if enabled
                        string fullContent = string.Empty;
                        if (_options.FetchFullContent)
                        {
                            fullContent = await FetchArticleContentAsync(link, cancellationToken);
                        }
                        
                        // Use the classifier to categorize the article
                        string category = await _categoryClassifier.ClassifyArticleAsync(
                            title, 
                            string.IsNullOrWhiteSpace(fullContent) ? cleanDescription : fullContent,
                            source.Name,
                            sourceCategory);
                        
                        // Create article
                        var article = NewsArticle.Create(
                            title,
                            cleanDescription,
                            string.IsNullOrWhiteSpace(fullContent) ? cleanDescription : fullContent,
                            source,
                            pubDate,
                            category,
                            link);
                            
                        articles.Add(article);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing article from source: {SourceName}", source.Name);
                    }
                }
                
                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching articles from source: {SourceName}", source.Name);
                return articles;
            }
        }
        
        private async Task<string> FetchArticleContentAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                // Use HtmlAgilityPack to parse the HTML
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);
                
                // Common selectors for article content - these might need adjustment based on the site
                var contentSelectors = new[] 
                {
                    "//article", 
                    "//div[contains(@class, 'article-body')]",
                    "//div[contains(@class, 'entry-content')]",
                    "//div[contains(@class, 'post-content')]",
                    "//div[contains(@class, 'content')]",
                    "//main"
                };
                
                // Try each selector to find content
                foreach (var selector in contentSelectors)
                {
                    var contentNode = htmlDoc.DocumentNode.SelectSingleNode(selector);
                    if (contentNode != null)
                    {
                        return CleanHtml(contentNode.InnerText);
                    }
                }
                
                // If no content found with selectors, return empty string
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching article content from URL: {Url}", url);
                return string.Empty;
            }
        }
        
        private string CleanHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;
                
            try
            {
                // Use HtmlAgilityPack to clean HTML
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                
                // Get text without HTML tags and decode HTML entities
                var text = htmlDoc.DocumentNode.InnerText;
                
                // Replace multiple spaces, newlines, etc. with a single space
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
                
                return text.Trim();
            }
            catch
            {
                // If HTML parsing fails, do a simple strip of tags
                return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
            }
        }
    }
} 