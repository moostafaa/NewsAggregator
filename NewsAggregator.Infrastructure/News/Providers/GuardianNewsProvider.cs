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
    public class GuardianNewsProvider : INewsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly NewsSourceConfig _defaultConfig;
        private static readonly Dictionary<string, string> DefaultCategories = new()
        {
            { "world", "world" },
            { "politics", "politics" },
            { "business", "business" },
            { "technology", "technology" },
            { "sport", "sports" },
            { "culture", "culture" },
            { "lifestyle", "lifestyle" },
            { "environment", "environment" }
        };

        public NewsProviderType ProviderType => NewsProviderType.TheGuardian;

        public GuardianNewsProvider(HttpClient httpClient, NewsSourceConfig defaultConfig = null)
        {
            _httpClient = httpClient;
            _defaultConfig = defaultConfig ?? new NewsSourceConfig
            {
                ProviderType = NewsProviderType.TheGuardian,
                BaseUrl = "https://content.guardianapis.com",
                ApiKey = Environment.GetEnvironmentVariable("GUARDIAN_API_KEY") ?? "test-api-key"
            };
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestNewsAsync(string category = null, int count = 20)
        {
            return await FetchNewsAsync(_defaultConfig, category, count);
        }

        public async Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string category = null, int count = 20)
        {
            try
            {
                var url = BuildSearchUrl(_defaultConfig, query, category, count);
                var response = await _httpClient.GetStringAsync(url);
                var guardianResponse = JsonSerializer.Deserialize<GuardianResponse>(response);

                return guardianResponse.Response.Results.Select(item => NewsArticle.Create(
                    item.WebTitle,
                    item.Fields?.TrailText ?? "",
                    item.Fields?.BodyText ?? "",
                    NewsSource.Create(
                        "The Guardian",
                        item.WebUrl,
                        new[] { item.SectionName.ToLowerInvariant() }
                    ),
                    DateTime.Parse(item.WebPublicationDate),
                    item.SectionId.ToLowerInvariant(),
                    item.WebUrl,
                    null
                ));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching news from The Guardian: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<NewsArticle>> FetchNewsAsync(NewsSourceConfig config, string category = null, int? limit = null)
        {
            try
            {
                var url = BuildApiUrl(config, category, limit);
                var response = await _httpClient.GetStringAsync(url);
                var guardianResponse = JsonSerializer.Deserialize<GuardianResponse>(response);

                return guardianResponse.Response.Results.Select(item => NewsArticle.Create(
                    item.WebTitle,
                    item.Fields?.TrailText ?? "",
                    item.Fields?.BodyText ?? "",
                    NewsSource.Create(
                        "The Guardian",
                        item.WebUrl,
                        new[] { item.SectionName.ToLowerInvariant() }
                    ),
                    DateTime.Parse(item.WebPublicationDate),
                    item.SectionId.ToLowerInvariant(),
                    item.WebUrl,
                    null
                ));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching news from The Guardian: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableCategoriesAsync(NewsSourceConfig config)
        {
            return DefaultCategories.Keys;
        }

        public async Task<bool> ValidateConfigurationAsync(NewsSourceConfig config)
        {
            try
            {
                var url = $"{config.BaseUrl}/search?api-key={config.ApiKey}&page-size=1";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string BuildApiUrl(NewsSourceConfig config, string category, int? limit)
        {
            var baseUrl = $"{config.BaseUrl}/search?api-key={config.ApiKey}&show-fields=trailText,bodyText";

            if (!string.IsNullOrEmpty(category) && DefaultCategories.ContainsKey(category.ToLower()))
            {
                baseUrl += $"&section={category.ToLower()}";
            }

            if (limit.HasValue)
            {
                baseUrl += $"&page-size={limit.Value}";
            }

            return baseUrl;
        }

        private string BuildSearchUrl(NewsSourceConfig config, string query, string category, int limit)
        {
            var baseUrl = $"{config.BaseUrl}/search?api-key={config.ApiKey}&q={Uri.EscapeDataString(query)}&show-fields=trailText,bodyText&page-size={limit}";

            if (!string.IsNullOrEmpty(category) && DefaultCategories.ContainsKey(category.ToLower()))
            {
                baseUrl += $"&section={category.ToLower()}";
            }

            return baseUrl;
        }

        private class GuardianResponse
        {
            public GuardianResponseContent Response { get; set; }
        }

        private class GuardianResponseContent
        {
            public string Status { get; set; }
            public string UserTier { get; set; }
            public int Total { get; set; }
            public int StartIndex { get; set; }
            public int PageSize { get; set; }
            public int CurrentPage { get; set; }
            public int Pages { get; set; }
            public List<GuardianArticle> Results { get; set; }
        }

        private class GuardianArticle
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string SectionId { get; set; }
            public string SectionName { get; set; }
            public string WebPublicationDate { get; set; }
            public string WebTitle { get; set; }
            public string WebUrl { get; set; }
            public string ApiUrl { get; set; }
            public bool IsHosted { get; set; }
            public string PillarId { get; set; }
            public string PillarName { get; set; }
            public GuardianFields Fields { get; set; }
        }

        private class GuardianFields
        {
            public string TrailText { get; set; }
            public string BodyText { get; set; }
        }
    }
} 