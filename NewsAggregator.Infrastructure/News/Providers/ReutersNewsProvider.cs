using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Infrastructure.News.Providers
{
    public class ReutersNewsProvider : INewsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly INewsCategoryService _categoryService;

        public NewsProviderType ProviderType => NewsProviderType.Reuters;

        public ReutersNewsProvider(HttpClient httpClient, INewsCategoryService categoryService)
        {
            _httpClient = httpClient;
            _categoryService = categoryService;
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestNewsAsync(string category = null, int count = 20)
        {
            // This would typically be implemented with a real API key and configuration
            // For now, we'll just call FetchNewsAsync with a basic config
            var config = new NewsSourceConfig
            {
                ApiKey = "dummy-key",
                BaseUrl = "https://api.reuters.com/v2"
            };
            
            return await FetchNewsAsync(config, category, count);
        }

        public async Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string category = null, int count = 20)
        {
            try
            {
                var config = new NewsSourceConfig
                {
                    ApiKey = "dummy-key",
                    BaseUrl = "https://api.reuters.com/v2"
                };
                
                var url = await BuildSearchUrl(config, query, category, count);
                _httpClient.DefaultRequestHeaders.Add("api-key", config.ApiKey);
                var response = await _httpClient.GetStringAsync(url);
                var reutersResponse = JsonSerializer.Deserialize<ReutersResponse>(response);

                var categoryObj = !string.IsNullOrEmpty(category) 
                    ? await _categoryService.GetBySlugAsync(category)
                    : null;

                return reutersResponse.Articles.Select(article => NewsArticle.Create(
                    article.Title,
                    article.Description ?? article.Summary ?? "",
                    article.Body ?? "",
                    NewsSource.Create(
                        "Reuters",
                        article.CanonicalUrl,
                        new[] { category ?? article.Channel ?? "general" }
                    ),
                    article.PublishedAt,
                    category ?? article.Channel ?? "general",
                    article.CanonicalUrl,
                    article.Keywords
                ));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching news from Reuters: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<NewsArticle>> FetchNewsAsync(NewsSourceConfig config, string category = null, int? limit = null)
        {
            try
            {
                var url = await BuildApiUrl(config, category, limit);
                _httpClient.DefaultRequestHeaders.Add("api-key", config.ApiKey);
                var response = await _httpClient.GetStringAsync(url);
                var reutersResponse = JsonSerializer.Deserialize<ReutersResponse>(response);

                var categoryObj = !string.IsNullOrEmpty(category) 
                    ? await _categoryService.GetBySlugAsync(category)
                    : null;

                return reutersResponse.Articles.Select(article => NewsArticle.Create(
                    article.Title,
                    article.Description ?? article.Summary ?? "",
                    article.Body ?? "",
                    NewsSource.Create(
                        "Reuters",
                        article.CanonicalUrl,
                        new[] { category ?? article.Channel ?? "general" }
                    ),
                    article.PublishedAt,
                    category ?? article.Channel ?? "general",
                    article.CanonicalUrl,
                    article.Keywords
                ));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching news from Reuters: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableCategoriesAsync(NewsSourceConfig config)
        {
            var categories = await _categoryService.GetActiveByProviderTypeAsync(ProviderType);
            return categories.Select(c => c.Slug);
        }

        public async Task<bool> ValidateConfigurationAsync(NewsSourceConfig config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("api-key", config.ApiKey);
                var url = $"{config.BaseUrl}/articles?limit=1";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> BuildApiUrl(NewsSourceConfig config, string category, int? limit)
        {
            var baseUrl = $"{config.BaseUrl}/articles";
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(category))
            {
                var categoryObj = await _categoryService.GetBySlugAsync(category);
                if (categoryObj != null && categoryObj.ProviderType == ProviderType)
                {
                    queryParams.Add($"channel={categoryObj.ProviderSpecificKey}");
                }
            }

            if (limit.HasValue)
            {
                queryParams.Add($"limit={limit.Value}");
            }

            // Add default sorting by newest first
            queryParams.Add("sortBy=published_at");
            queryParams.Add("sortOrder=desc");

            return queryParams.Count > 0
                ? $"{baseUrl}?{string.Join("&", queryParams)}"
                : baseUrl;
        }

        private async Task<string> BuildSearchUrl(NewsSourceConfig config, string query, string category, int count)
        {
            var baseUrl = $"{config.BaseUrl}/search/articles";
            var queryParams = new List<string>();
            
            queryParams.Add($"keyword={Uri.EscapeDataString(query)}");
            
            if (!string.IsNullOrEmpty(category))
            {
                var categoryObj = await _categoryService.GetBySlugAsync(category);
                if (categoryObj != null && categoryObj.ProviderType == ProviderType)
                {
                    queryParams.Add($"channel={categoryObj.ProviderSpecificKey}");
                }
            }
            
            if (count > 0)
            {
                queryParams.Add($"limit={count}");
            }
            
            // Add default sorting by newest first
            queryParams.Add("sortBy=published_at");
            queryParams.Add("sortOrder=desc");
            
            return $"{baseUrl}?{string.Join("&", queryParams)}";
        }

        private class ReutersResponse
        {
            public List<ReutersArticle> Articles { get; set; }
            public int TotalHits { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        private class ReutersArticle
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Summary { get; set; }
            public string Body { get; set; }
            public string CanonicalUrl { get; set; }
            public DateTime PublishedAt { get; set; }
            public string Channel { get; set; }
            public List<string> Keywords { get; set; }
            public string AuthorName { get; set; }
            public string AuthorTitle { get; set; }
        }
    }
} 