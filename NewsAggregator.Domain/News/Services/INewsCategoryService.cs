using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Services
{
    public interface INewsCategoryService
    {
        Task<NewsCategory> GetByIdAsync(Guid id);
        Task<NewsCategory> GetBySlugAsync(string slug);
        Task<IEnumerable<NewsCategory>> GetAllAsync();
        Task<IEnumerable<NewsCategory>> GetByProviderTypeAsync(NewsProviderType providerType);
        Task<IEnumerable<NewsCategory>> GetActiveByProviderTypeAsync(NewsProviderType providerType);
        Task<NewsCategory> CreateAsync(string name, string description, NewsProviderType providerType, string providerSpecificKey);
        Task<NewsCategory> UpdateAsync(Guid id, string name, string description, string providerSpecificKey, bool isActive);
        Task<bool> DeleteAsync(Guid id);
        Task<NewsCategory> ActivateAsync(Guid id);
        Task<NewsCategory> DeactivateAsync(Guid id);
    }
} 