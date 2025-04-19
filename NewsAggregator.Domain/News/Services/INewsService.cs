using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Services
{
    public interface INewsService
    {
        Task<IEnumerable<NewsArticle>> GetNewsAsync(string provider = null, string category = null, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string provider = null, string category = null);
        Task<IEnumerable<NewsProviderInfo>> GetAvailableProvidersAsync();
        Task<IEnumerable<NewsArticle>> GetTrendingNewsAsync(string provider = null, int limit = 5);
        Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string provider = null, string category = null, int page = 1, int pageSize = 10);
        Task<int> GetSearchTotalCountAsync(string query, string provider = null, string category = null);
    }

    public class NewsProviderInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public bool IsEnabled { get; set; }
    }
} 