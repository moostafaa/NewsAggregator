using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Service that fetches news sources from the main application's API
    /// </summary>
    public class ApiSourceService : ISourceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiSourceService> _logger;
        private readonly DistributedCrawlerOptions _options;
        
        public ApiSourceService(
            HttpClient httpClient,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<ApiSourceService> logger)
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
        
        public async Task<IEnumerable<NewsSource>> GetAllSourcesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/sources", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                var sources = await JsonSerializer.DeserializeAsync<List<NewsSource>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
                    cancellationToken);
                
                _logger.LogInformation("Retrieved {Count} sources from API", sources.Count);
                return sources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sources from API");
                return new List<NewsSource>();
            }
        }
        
        public async Task<IEnumerable<NewsSource>> GetSourcesByFilterAsync(
            string category = null, 
            string providerType = null, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build the query parameters
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(category))
                {
                    queryParams.Add($"category={Uri.EscapeDataString(category)}");
                }
                
                if (!string.IsNullOrEmpty(providerType))
                {
                    queryParams.Add($"providerType={Uri.EscapeDataString(providerType)}");
                }
                
                queryParams.Add($"limit={limit}");
                
                var queryString = string.Join("&", queryParams);
                var url = $"api/sources/filter?{queryString}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                var sources = await JsonSerializer.DeserializeAsync<List<NewsSource>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
                    cancellationToken);
                
                _logger.LogInformation("Retrieved {Count} filtered sources from API", sources.Count);
                return sources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered sources from API");
                return new List<NewsSource>();
            }
        }
    }
} 