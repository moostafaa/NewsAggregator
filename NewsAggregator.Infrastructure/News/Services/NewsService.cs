using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class NewsService : INewsService
    {
        private readonly IEnumerable<INewsProvider> _newsProviders;
        private readonly INewsCategoryService _categoryService;

        public NewsService(IEnumerable<INewsProvider> newsProviders, INewsCategoryService categoryService)
        {
            _newsProviders = newsProviders;
            _categoryService = categoryService;
        }

        public async Task<IEnumerable<NewsArticle>> GetNewsAsync(string provider = null, string category = null, int page = 1, int pageSize = 10)
        {
            var providers = GetSelectedProviders(provider);
            var tasks = new List<Task<IEnumerable<NewsArticle>>>();

            foreach (var newsProvider in providers)
            {
                tasks.Add(FetchNewsFromProvider(newsProvider, category, pageSize));
            }

            var allArticles = (await Task.WhenAll(tasks))
                .SelectMany(x => x)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return allArticles;
        }

        public async Task<int> GetTotalCountAsync(string provider = null, string category = null)
        {
            var providers = GetSelectedProviders(provider);
            var tasks = new List<Task<IEnumerable<NewsArticle>>>();

            foreach (var newsProvider in providers)
            {
                tasks.Add(FetchNewsFromProvider(newsProvider, category, int.MaxValue));
            }

            var totalCount = (await Task.WhenAll(tasks))
                .SelectMany(x => x)
                .Count();

            return totalCount;
        }

        public async Task<IEnumerable<NewsProviderInfo>> GetAvailableProvidersAsync()
        {
            return _newsProviders.Select(p => new NewsProviderInfo
            {
                Name = p.ProviderType.ToString(),
                Description = GetProviderDescription(p.ProviderType),
                LogoUrl = GetProviderLogoUrl(p.ProviderType),
                IsEnabled = true
            });
        }

        public async Task<IEnumerable<NewsArticle>> GetTrendingNewsAsync(string provider = null, int limit = 5)
        {
            var providers = GetSelectedProviders(provider);
            var tasks = new List<Task<IEnumerable<NewsArticle>>>();

            foreach (var newsProvider in providers)
            {
                tasks.Add(FetchNewsFromProvider(newsProvider, null, limit));
            }

            var trendingArticles = (await Task.WhenAll(tasks))
                .SelectMany(x => x)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit);

            return trendingArticles;
        }

        public async Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string provider = null, string category = null, int page = 1, int pageSize = 10)
        {
            // For now, we'll just filter the results from GetNewsAsync
            // In a real implementation, you would pass the search query to the providers
            var allArticles = await GetNewsAsync(provider, category, 1, int.MaxValue);
            
            var searchResults = allArticles
                .Where(a => 
                    a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    //a.Content.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return searchResults;
        }

        public async Task<int> GetSearchTotalCountAsync(string query, string provider = null, string category = null)
        {
            var allArticles = await GetNewsAsync(provider, category, 1, int.MaxValue);
            
            var totalCount = allArticles
                .Count(a => 
                    a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    //a.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.Body.Contains(query, StringComparison.OrdinalIgnoreCase));

            return totalCount;
        }

        private IEnumerable<INewsProvider> GetSelectedProviders(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return _newsProviders;

            if (Enum.TryParse<NewsProviderType>(provider, true, out var providerType))
            {
                return _newsProviders.Where(p => p.ProviderType == providerType);
            }

            return _newsProviders;
        }

        private async Task<IEnumerable<NewsArticle>> FetchNewsFromProvider(INewsProvider provider, string category, int limit)
        {
            try
            {
                var config = GetProviderConfig(provider.ProviderType);
                return await provider.FetchNewsAsync(config, category, limit);
            }
            catch (Exception)
            {
                return Enumerable.Empty<NewsArticle>();
            }
        }

        private NewsSourceConfig GetProviderConfig(NewsProviderType providerType)
        {
            // In a real implementation, you would get this from configuration or database
            return new NewsSourceConfig
            {
                ApiKey = GetApiKey(providerType),
                BaseUrl = GetBaseUrl(providerType)
            };
        }

        private string GetApiKey(NewsProviderType providerType)
        {
            // In a real implementation, you would get this from configuration or secrets
            return providerType switch
            {
                NewsProviderType.NewsAPI => "your-newsapi-key",
                NewsProviderType.TheGuardian => "your-guardian-key",
                NewsProviderType.Reuters => "your-reuters-key",
                NewsProviderType.AssociatedPress => "your-ap-key",
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
        }

        private string GetBaseUrl(NewsProviderType providerType)
        {
            // In a real implementation, you would get this from configuration
            return providerType switch
            {
                NewsProviderType.NewsAPI => "https://newsapi.org/v2",
                NewsProviderType.TheGuardian => "https://content.guardianapis.com",
                NewsProviderType.Reuters => "https://api.reuters.com/v2",
                NewsProviderType.AssociatedPress => "https://api.ap.org/v2",
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
        }

        private string GetProviderDescription(NewsProviderType providerType)
        {
            return providerType switch
            {
                NewsProviderType.NewsAPI => "News from various sources worldwide",
                NewsProviderType.TheGuardian => "Quality independent journalism",
                NewsProviderType.Reuters => "Trusted international news agency",
                NewsProviderType.AssociatedPress => "Independent global news organization",
                _ => string.Empty
            };
        }

        private string GetProviderLogoUrl(NewsProviderType providerType)
        {
            // In a real implementation, you would get this from configuration or assets
            return providerType switch
            {
                NewsProviderType.NewsAPI => "/images/providers/newsapi.png",
                NewsProviderType.TheGuardian => "/images/providers/guardian.png",
                NewsProviderType.Reuters => "/images/providers/reuters.png",
                NewsProviderType.AssociatedPress => "/images/providers/ap.png",
                _ => string.Empty
            };
        }
    }
} 