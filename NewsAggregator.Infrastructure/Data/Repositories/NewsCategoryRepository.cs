using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Repositories;

namespace NewsAggregator.Infrastructure.Data.Repositories
{
    public class NewsCategoryRepository : INewsCategoryRepository
    {
        private readonly NewsAggregatorDbContext _context;

        public NewsCategoryRepository(NewsAggregatorDbContext context)
        {
            _context = context;
        }

        public async Task<NewsCategory> GetByIdAsync(Guid id)
        {
            return await _context.NewsCategories.FindAsync(id);
        }

        public async Task<NewsCategory> GetBySlugAsync(string slug)
        {
            return await _context.NewsCategories
                .FirstOrDefaultAsync(x => x.Slug == slug);
        }

        public async Task<IEnumerable<NewsCategory>> GetAllAsync()
        {
            return await _context.NewsCategories.ToListAsync();
        }

        public async Task<IEnumerable<NewsCategory>> GetActiveAsync()
        {
            return await _context.NewsCategories
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsWithNameAsync(string name)
        {
            return await _context.NewsCategories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsWithSlugAsync(string slug)
        {
            return await _context.NewsCategories
                .AnyAsync(c => c.Slug.ToLower() == slug.ToLower());
        }

        public async Task<IEnumerable<NewsCategory>> GetByProviderTypeAsync(NewsProviderType providerType)
        {
            return await _context.NewsCategories
                .Where(x => x.ProviderType == providerType)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<NewsCategory>> GetActiveByProviderTypeAsync(NewsProviderType providerType)
        {
            return await _context.NewsCategories
                .Where(x => x.ProviderType == providerType && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task AddAsync(NewsCategory category)
        {
            await _context.NewsCategories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NewsCategory category)
        {
            _context.NewsCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(NewsCategory category)
        {
            _context.NewsCategories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await GetByIdAsync(id);
            if (category != null)
            {
                _context.NewsCategories.Remove(category);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<NewsCategory> GetByNameAsync(string name)
        {
            return await _context.NewsCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }
    }
} 