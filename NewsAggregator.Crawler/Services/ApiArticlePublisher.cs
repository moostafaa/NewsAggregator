using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Service that publishes articles back to the main application via its API
    /// </summary>
    public class ApiArticlePublisher : IArticlePublisher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiArticlePublisher> _logger;
        private readonly DistributedCrawlerOptions _options;
        
        public ApiArticlePublisher(
            HttpClient httpClient,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<ApiArticlePublisher> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            // Configure the HTTP client
            _httpClient.BaseAddress = new Uri(_options.ApiEndpoint);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
            }
        }
        
        public async Task<bool> PublishArticleAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/articles", article, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully published article: {Title}", article.Title);
                    return true;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to publish article: {Title}. Status: {Status}, Error: {Error}", 
                        article.Title, response.StatusCode, errorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing article: {Title}", article.Title);
                return false;
            }
        }
        
        public async Task<int> PublishArticlesAsync(IEnumerable<NewsArticle> articles, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/articles/batch", articles, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<BatchPublishResult>(cancellationToken);
                    _logger.LogInformation("Published {SuccessCount} articles in batch, {FailedCount} failed", 
                        result.SuccessCount, result.FailedCount);
                    
                    return result.SuccessCount;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to publish article batch. Status: {Status}, Error: {Error}", 
                        response.StatusCode, errorMessage);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing article batch");
                return 0;
            }
        }
        
        public async Task ReportCrawlerStatsAsync(
            string crawlerName, 
            int sourcesProcessed, 
            int articlesPublished, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = new
                {
                    CrawlerName = crawlerName,
                    SourcesProcessed = sourcesProcessed,
                    ArticlesPublished = articlesPublished,
                    Timestamp = DateTime.UtcNow
                };
                
                var response = await _httpClient.PostAsJsonAsync("api/crawlers/stats", stats, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully reported crawler stats for {CrawlerName}", crawlerName);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to report crawler stats. Status: {Status}, Error: {Error}", 
                        response.StatusCode, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting crawler stats for {CrawlerName}", crawlerName);
            }
        }
        
        private class BatchPublishResult
        {
            public int SuccessCount { get; set; }
            public int FailedCount { get; set; }
            public List<string> Errors { get; set; }
        }
    }
} 