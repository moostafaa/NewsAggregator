using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;
using NewsAggregator.Infrastructure.Options;
using System.Collections.Concurrent;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class NewsCrawlerService : INewsCrawlerService
    {
        private readonly HttpClient _httpClient;
        private readonly ICategoryClassificationService _categoryClassifier;
        private readonly ILogger<NewsCrawlerService> _logger;
        private readonly List<NewsSource> _sources;
        private readonly CrawlerOptions _options;

        public NewsCrawlerService(
            HttpClient httpClient,
            ICategoryClassificationService categoryClassifier,
            IOptions<CrawlerOptions> options,
            ILogger<NewsCrawlerService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _categoryClassifier = categoryClassifier ?? throw new ArgumentNullException(nameof(categoryClassifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _sources = new List<NewsSource>();
        }

        public async Task<IEnumerable<NewsArticle>> FetchArticlesFromSourceAsync(
            NewsSource source, 
            int maxArticles = 20, 
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
                
            _logger.LogInformation("Fetching articles from source: {SourceName}", source.Name);
            
            try
            {
                var articles = new List<NewsArticle>();
                
                // Fetch the RSS feed
                var response = await _httpClient.GetStringAsync(source.Url, cancellationToken);
                var feed = XDocument.Parse(response);
                
                // Use XML namespaces to support different RSS formats
                var ns = feed.Root.GetDefaultNamespace();
                
                // Extract the channel element (might depend on the RSS format)
                var channel = feed.Descendants(ns + "channel").FirstOrDefault();
                if (channel == null)
                {
                    _logger.LogWarning("Unable to find channel element in RSS feed for source: {SourceName}", source.Name);
                    return articles;
                }
                
                // Extract the items from the feed
                var items = channel.Elements(ns + "item").Take(maxArticles);
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
                        
                        // Fetch full content if needed
                        string fullContent = await FetchArticleContentAsync(link, cancellationToken);
                        
                        // Use DeepSeek to categorize the article
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
                        
                        _logger.LogInformation("Fetched article: {Title}, Category: {Category}", title, category);
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
                return Enumerable.Empty<NewsArticle>();
            }
        }

        public async Task<IEnumerable<NewsArticle>> FetchArticlesFromAllSourcesAsync(
            int maxArticlesPerSource = 10, 
            CancellationToken cancellationToken = default)
        {
            // Use ConcurrentBag for thread-safe collection
            var allArticles = new ConcurrentBag<NewsArticle>();
            
            // Get max degree of parallelism from options, default to processor count if not specified
            int maxDegreeOfParallelism = _options.MaxConcurrentSources > 0 
                ? _options.MaxConcurrentSources 
                : Environment.ProcessorCount;
                
            _logger.LogInformation("Fetching articles from {Count} sources with parallelism of {Parallelism}", 
                _sources.Count, maxDegreeOfParallelism);
            
            // Use SemaphoreSlim to control concurrency
            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();
            
            foreach (var source in _sources)
            {
                // Wait for a slot to be available
                await semaphore.WaitAsync(cancellationToken);
                
                // Create a task for each source
                tasks.Add(Task.Run(async () => 
                {
                    try
                    {
                        var articles = await FetchArticlesFromSourceAsync(
                            source, 
                            maxArticlesPerSource, 
                            cancellationToken);
                            
                        // Add articles to the concurrent collection
                        foreach (var article in articles)
                        {
                            allArticles.Add(article);
                        }
                        
                        _logger.LogInformation("Completed fetching from source: {SourceName}, added {Count} articles", 
                            source.Name, articles.Count());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching articles from source: {SourceName}", source.Name);
                    }
                    finally
                    {
                        // Release the semaphore slot
                        semaphore.Release();
                    }
                }, cancellationToken));
            }
            
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Completed fetching from all sources, total articles: {Count}", allArticles.Count);
            return allArticles;
        }

        public Task AddSourceAsync(NewsSource source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
                
            if (!_sources.Any(s => s.Url.Equals(source.Url)))
            {
                _sources.Add(source);
                _logger.LogInformation("Added source: {SourceName}", source.Name);
            }
            
            return Task.CompletedTask;
        }

        public Task RemoveSourceAsync(string sourceUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new ArgumentException("Source URL cannot be empty", nameof(sourceUrl));
                
            var source = _sources.FirstOrDefault(s => s.Url.ToString().Equals(sourceUrl, StringComparison.OrdinalIgnoreCase));
            if (source != null)
            {
                _sources.Remove(source);
                _logger.LogInformation("Removed source: {SourceName}", source.Name);
            }
            
            return Task.CompletedTask;
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