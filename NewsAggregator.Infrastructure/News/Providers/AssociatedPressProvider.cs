using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Infrastructure.News.Providers
{
    public class AssociatedPressProvider : INewsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly INewsCategoryRepository _categoryRepository;
        private readonly ICategoryClassificationService _categoryClassifier;

        public NewsProviderType ProviderType => NewsProviderType.AssociatedPress;

        public AssociatedPressProvider(
            HttpClient httpClient,
            INewsCategoryRepository categoryRepository,
            ICategoryClassificationService categoryClassifier)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _categoryClassifier = categoryClassifier ?? throw new ArgumentNullException(nameof(categoryClassifier));
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestNewsAsync(string category = null, int count = 20)
        {
            // Create a basic configuration for the API call
            var config = new NewsSourceConfig
            {
                ApiKey = "dummy-key",
                BaseUrl = "https://api.ap.org/data"
            };
            
            return await FetchNewsAsync(config, category, count);
        }
        
        public async Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string category = null, int count = 20)
        {
            var config = new NewsSourceConfig
            {
                ApiKey = "dummy-key",
                BaseUrl = "https://api.ap.org/data"
            };
            
            try
            {
                var baseUrl = $"{config.BaseUrl}/v2/search?q={Uri.EscapeDataString(query)}";
                
                if (count > 0)
                {
                    baseUrl += $"&limit={count}";
                }
                
                _httpClient.DefaultRequestHeaders.Add("apikey", config.ApiKey);
                var response = await _httpClient.GetStringAsync(baseUrl);
                var apResponse = JsonSerializer.Deserialize<APResponse>(response);

                var articles = new List<NewsArticle>();
                
                foreach (var article in apResponse.Items)
                {
                    // Use DeepSeek to classify the article
                    string classifiedCategory = await _categoryClassifier.ClassifyArticleAsync(
                        article.Headline,
                        article.Abstract + " " + article.Body,
                        "Associated Press",
                        article.Category
                    );
                    
                    var newsArticle = NewsArticle.Create(
                        article.Headline,
                        article.Abstract ?? string.Empty,
                        article.Body ?? article.Abstract ?? string.Empty,
                        NewsSource.Create(
                            "Associated Press",
                            article.ItemUrl,
                            new[] { classifiedCategory }
                        ),
                        article.PublishedDate,
                        classifiedCategory,
                        article.ItemUrl
                    );
                    
                    articles.Add(newsArticle);
                }

                return articles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching news from Associated Press: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<NewsArticle>> FetchNewsAsync(NewsSourceConfig config, string category = null, int? limit = null)
        {
            try
            {
                var url = await BuildApiUrl(config, category, limit);
                _httpClient.DefaultRequestHeaders.Add("apikey", config.ApiKey);
                var response = await _httpClient.GetStringAsync(url);
                var apResponse = JsonSerializer.Deserialize<APResponse>(response);

                var articles = new List<NewsArticle>();
                
                foreach (var article in apResponse.Items)
                {
                    // Get source category from article or use the requested category
                    var sourceCategory = article.Category ?? category;
                    
                    // Use DeepSeek to classify the article
                    string classifiedCategory = await _categoryClassifier.ClassifyArticleAsync(
                        article.Headline,
                        article.Abstract + " " + article.Body,
                        "Associated Press",
                        sourceCategory
                    );
                    
                    var newsArticle = NewsArticle.Create(
                        article.Headline,
                        article.Abstract ?? string.Empty,
                        article.Body ?? article.Abstract ?? string.Empty,
                        NewsSource.Create(
                            "Associated Press",
                            article.ItemUrl,
                            new[] { classifiedCategory }
                        ),
                        article.PublishedDate,
                        classifiedCategory,
                        article.ItemUrl
                    );
                    
                    articles.Add(newsArticle);
                }

                return articles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching news from Associated Press: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableCategoriesAsync(NewsSourceConfig config)
        {
            // Get categories from the database rather than hardcoded values
            var categories = await _categoryRepository.GetActiveAsync();
            return categories.Select(c => c.Name.ToLowerInvariant());
        }

        public async Task<bool> ValidateConfigurationAsync(NewsSourceConfig config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("apikey", config.ApiKey);
                var url = $"{config.BaseUrl}/v2/items?limit=1";
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
            var baseUrl = $"{config.BaseUrl}/v2/items";
            var queryParams = new List<string>();

            // Only add a category if it's valid
            if (!string.IsNullOrEmpty(category))
            {
                // Get valid categories from the database
                var validCategories = await GetAvailableCategoriesAsync(config);
                
                // Map to AP category if possible, otherwise just search for it
                var apCategory = MapCategoryToAP(category);
                if (!string.IsNullOrEmpty(apCategory))
                {
                    queryParams.Add($"category={apCategory}");
                }
                else
                {
                    // If can't map directly, search by the category as a keyword
                    queryParams.Add($"q={Uri.EscapeDataString(category)}");
                }
            }

            if (limit.HasValue)
            {
                queryParams.Add($"limit={limit.Value}");
            }

            // Add default parameters
            queryParams.Add("format=json");
            queryParams.Add("sortBy=published");
            queryParams.Add("sortOrder=desc");

            return queryParams.Count > 0
                ? $"{baseUrl}?{string.Join("&", queryParams)}"
                : baseUrl;
        }
        
        // Map our system categories to AP categories
        private string MapCategoryToAP(string category)
        {
            var apCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "politics", "politics" },
                { "business", "business" },
                { "technology", "technology" },
                { "science", "science" },
                { "health", "health" },
                { "sports", "sports" },
                { "entertainment", "entertainment" },
                // Map additional categories
                { "world", "international" },
                { "environment", "science" },
                { "lifestyle", "lifestyle" },
                { "food", "lifestyle" },
                { "travel", "lifestyle" },
                { "opinion", "commentary" },
                { "education", "domestic" }
            };
            
            return apCategories.TryGetValue(category, out var apCategory) ? apCategory : string.Empty;
        }

        private class APResponse
        {
            public List<APArticle> Items { get; set; }
            public int TotalResults { get; set; }
            public int Offset { get; set; }
            public int Limit { get; set; }
        }

        private class APArticle
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Headline { get; set; }
            public string Abstract { get; set; }
            public string Body { get; set; }
            public string ItemUrl { get; set; }
            public DateTime PublishedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
            public string Category { get; set; }
            public List<string> Keywords { get; set; }
            public List<APByline> Bylines { get; set; }
            public List<APMedia> Media { get; set; }
        }

        private class APByline
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public string Organization { get; set; }
        }

        private class APMedia
        {
            public string Type { get; set; }
            public string SubType { get; set; }
            public string Url { get; set; }
            public string Caption { get; set; }
            public string Credit { get; set; }
        }
    }
} 