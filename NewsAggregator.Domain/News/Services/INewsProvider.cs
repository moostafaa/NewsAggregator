using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.News.Services
{
    public interface INewsProvider
    {
        NewsProviderType ProviderType { get; }
        Task<IEnumerable<NewsArticle>> GetLatestNewsAsync(string category = null, int count = 20);
        Task<IEnumerable<NewsArticle>> SearchNewsAsync(string query, string category = null, int count = 20);
        Task<IEnumerable<NewsArticle>> FetchNewsAsync(NewsSourceConfig config, string category = null, int? limit = null);
        Task<IEnumerable<string>> GetAvailableCategoriesAsync(NewsSourceConfig config);
        Task<bool> ValidateConfigurationAsync(NewsSourceConfig config);
    }

    public interface INewsProviderFactory
    {
        INewsProvider CreateProvider(NewsProviderType providerType);
    }
} 