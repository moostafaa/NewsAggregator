using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Repositories
{
    public interface INewsCategoryRepository : IRepository<NewsCategory>
    {
        Task<NewsCategory> GetBySlugAsync(string slug);
        Task<IEnumerable<NewsCategory>> GetByProviderTypeAsync(NewsProviderType providerType);
        Task<IEnumerable<NewsCategory>> GetActiveAsync();
        Task<IEnumerable<NewsCategory>> GetActiveByProviderTypeAsync(NewsProviderType providerType);
        Task<bool> ExistsWithNameAsync(string name);
        Task<bool> ExistsWithSlugAsync(string slug);
        
        // Added methods for gRPC service
        Task<NewsCategory> GetByNameAsync(string name);
        Task<IEnumerable<NewsCategory>> GetAllAsync();
    }
} 