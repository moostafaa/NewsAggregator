using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Infrastructure.News.Providers
{
    public class NewsApiProvider : INewsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly INewsCategoryRepository _categoryRepository;
        private readonly ICategoryClassificationService _categoryClassifier;

        public NewsProviderType ProviderType => NewsProviderType.NewsAPI;

        public NewsApiProvider(
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
            // This would typically be implemented with a real API key and configuration
            // For now, we'll just call FetchNewsAsync with a basic config
            var config = new NewsSourceConfig
            {
                ApiKey = "dummy-key",
                BaseUrl = "https://newsapi.org/v2"
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
                    BaseUrl = "https://newsapi.org/v2"
                };
                
                var baseUrl = $"{config.BaseUrl}/everything?q={Uri.EscapeDataString(query)}&language=en";
                
                if (count > 0)
                {
                    baseUrl += $"&pageSize={count}";
                }
                
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
                var response = await _httpClient.GetStringAsync(baseUrl);
                var newsApiResponse = JsonSerializer.Deserialize<NewsApiResponse>(response);

                var articles = new List<NewsArticle>();
                
                foreach (var article in newsApiResponse.Articles)
                {
                    // Use DeepSeek to classify the article
                    string classifiedCategory = await _categoryClassifier.ClassifyArticleAsync(
                        article.Title,
                        article.Description + " " + article.Content,
                        "NewsAPI",
                        article.Source.Name
                    );
                    
                    // Create the article with the classified category
                    var newsArticle = NewsArticle.Create(
                        article.Title,
                        article.Description ?? string.Empty,
                        article.Content ?? article.Description ?? string.Empty,
                        NewsSource.Create(article.Source.Name, article.Url, new[] { classifiedCategory }),
                        DateTime.Parse(article.PublishedAt),
                        classifiedCategory,
                        article.Url
                    );
                    
                    articles.Add(newsArticle);
                }

                return articles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching news from NewsAPI: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<NewsArticle>> FetchNewsAsync(NewsSourceConfig config, string category = null, int? limit = null)
        {
            try
            {
                var url = await BuildApiUrl(config, category, limit);
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);

                var response = await _httpClient.GetStringAsync(url);
                var newsApiResponse = JsonSerializer.Deserialize<NewsApiResponse>(response);

                var articles = new List<NewsArticle>();
                
                foreach (var article in newsApiResponse.Articles)
                {
                    // Use DeepSeek to classify the article
                    string classifiedCategory = await _categoryClassifier.ClassifyArticleAsync(
                        article.Title,
                        article.Description + " " + article.Content,
                        "NewsAPI",
                        category
                    );
                    
                    // Create the article with the classified category
                    var newsArticle = NewsArticle.Create(
                        article.Title,
                        article.Description ?? string.Empty,
                        article.Content ?? article.Description ?? string.Empty,
                        NewsSource.Create(article.Source.Name, article.Url, new[] { classifiedCategory }),
                        DateTime.Parse(article.PublishedAt),
                        classifiedCategory,
                        article.Url
                    );
                    
                    articles.Add(newsArticle);
                }

                return articles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching news from NewsAPI: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableCategoriesAsync(NewsSourceConfig config)
        {
            // Get categories from the database instead of hardcoded values
            var categories = await _categoryRepository.GetActiveAsync();
            return categories.Select(c => c.Name.ToLowerInvariant());
        }

        public async Task<bool> ValidateConfigurationAsync(NewsSourceConfig config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
                var response = await _httpClient.GetAsync($"{config.BaseUrl}/top-headlines?country=us&pageSize=1");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> BuildApiUrl(NewsSourceConfig config, string category, int? limit)
        {
            var baseUrl = $"{config.BaseUrl}/top-headlines?language=en";

            // Only add category if it's a valid one
            if (!string.IsNullOrEmpty(category))
            {
                // Get valid categories from database
                var validCategories = await GetAvailableCategoriesAsync(config);
                
                // Map the category to NewsAPI categories
                // NewsAPI has a limited set of categories
                var newsApiCategory = MapCategoryToNewsApi(category);
                
                if (!string.IsNullOrEmpty(newsApiCategory))
                {
                    baseUrl += $"&category={newsApiCategory}";
                }
            }

            if (limit.HasValue)
            {
                baseUrl += $"&pageSize={limit.Value}";
            }

            return baseUrl;
        }
        
        // Map our system categories to NewsAPI categories
        private string MapCategoryToNewsApi(string category)
        {
            // NewsAPI only supports these categories
            var newsApiCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "business", "business" },
                { "entertainment", "entertainment" },
                { "health", "health" },
                { "science", "science" },
                { "sports", "sports" },
                { "technology", "technology" },
                // Map our additional categories to NewsAPI categories
                { "politics", "general" },
                { "world", "general" },
                { "lifestyle", "general" },
                { "opinion", "general" },
                { "environment", "science" },
                { "education", "general" },
                { "travel", "general" },
                { "food", "general" }
            };
            
            return newsApiCategories.TryGetValue(category, out var newsApiCategory) 
                ? newsApiCategory 
                : "general"; // Default to general
        }

        private class NewsApiResponse
        {
            public string Status { get; set; }
            public int TotalResults { get; set; }
            public List<NewsApiArticle> Articles { get; set; }
        }

        private class NewsApiArticle
        {
            public NewsApiSource Source { get; set; }
            public string Author { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Url { get; set; }
            public string UrlToImage { get; set; }
            public string PublishedAt { get; set; }
            public string Content { get; set; }
        }

        private class NewsApiSource
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
} 