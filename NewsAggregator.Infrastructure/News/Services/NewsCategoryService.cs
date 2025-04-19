using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class NewsCategoryService : INewsCategoryService
    {
        private readonly INewsCategoryRepository _categoryRepository;

        public NewsCategoryService(INewsCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<NewsCategory> GetByIdAsync(Guid id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<NewsCategory> GetBySlugAsync(string slug)
        {
            return await _categoryRepository.GetBySlugAsync(slug);
        }

        public async Task<IEnumerable<NewsCategory>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<IEnumerable<NewsCategory>> GetByProviderTypeAsync(NewsProviderType providerType)
        {
            return await _categoryRepository.GetByProviderTypeAsync(providerType);
        }

        public async Task<IEnumerable<NewsCategory>> GetActiveByProviderTypeAsync(NewsProviderType providerType)
        {
            return await _categoryRepository.GetActiveByProviderTypeAsync(providerType);
        }

        public async Task<NewsCategory> CreateAsync(string name, string description, NewsProviderType providerType, string providerSpecificKey)
        {
            var slug = CreateSlug(name);
            var category = NewsCategory.Create(name, slug, description, providerType, providerSpecificKey);
            await _categoryRepository.AddAsync(category);
            return category;
        }

        public async Task<NewsCategory> UpdateAsync(Guid id, string name, string description, string providerSpecificKey, bool isActive)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new Exception($"Category with ID {id} not found");

            var slug = CreateSlug(name);
            category.Update(name, slug, description, category.ProviderType, providerSpecificKey);
            
            if (isActive)
                category.Activate();
            else
                category.Deactivate();
                
            await _categoryRepository.UpdateAsync(category);
            return category;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return false;
            
            await _categoryRepository.DeleteAsync(category);
            return true;
        }

        public async Task<NewsCategory> ActivateAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new Exception($"Category with ID {id} not found");

            category.Activate();
            await _categoryRepository.UpdateAsync(category);
            return category;
        }

        public async Task<NewsCategory> DeactivateAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new Exception($"Category with ID {id} not found");

            category.Deactivate();
            await _categoryRepository.UpdateAsync(category);
            return category;
        }

        private static string CreateSlug(string name)
        {
            return name.Trim()
                .ToLower()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("'", "")
                .Replace("\"", "");
        }
    }
} 