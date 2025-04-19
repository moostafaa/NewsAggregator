using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Infrastructure.Data;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Infrastructure.Data.Repositories
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly NewsAggregatorDbContext _context;

        public NewsArticleRepository(NewsAggregatorDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // IRepository<NewsArticle> implementation
        async Task<NewsArticle> IRepository<NewsArticle>.GetByIdAsync(Guid id)
        {
            return await GetByIdAsync(id);
        }

        async Task<IEnumerable<NewsArticle>> IRepository<NewsArticle>.GetAllAsync()
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .ToListAsync();
        }

        async Task IRepository<NewsArticle>.AddAsync(NewsArticle entity)
        {
            await AddAsync(entity);
        }

        async Task IRepository<NewsArticle>.UpdateAsync(NewsArticle entity)
        {
            await UpdateAsync(entity);
        }

        async Task IRepository<NewsArticle>.DeleteAsync(NewsArticle entity)
        {
            _context.NewsArticles.Remove(entity);
            await _context.SaveChangesAsync();
        }

        // INewsArticleRepository implementation
        public async Task<NewsArticle> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<NewsArticle> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .FirstOrDefaultAsync(a => a.Url.ToString() == url, cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetBySourceAsync(string sourceName, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .Where(a => a.Source.Name == sourceName)
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetByCategoryAsync(string category, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .Where(a => a.Category == category)
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetByTagAsync(string tag, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .Where(a => a.Tags.Contains(tag))
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetSavedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Include(a => a.Source)
                .Where(a => a.IsSaved)
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var normalizedQuery = query.ToLower();
            
            return await _context.NewsArticles
                .Include(a => a.Source)
                .Where(a => 
                    a.Title.ToLower().Contains(normalizedQuery) || 
                    a.Summary.ToLower().Contains(normalizedQuery) ||
                    (a.Body != null && a.Body.ToLower().Contains(normalizedQuery)))
                .OrderByDescending(a => a.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            await _context.NewsArticles.AddAsync(article, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            _context.NewsArticles.Update(article);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveOlderThanAsync(int days, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var articlesToRemove = await _context.NewsArticles
                .Where(a => a.PublishedDate < cutoffDate)
                .ToListAsync(cancellationToken);

            _context.NewsArticles.RemoveRange(articlesToRemove);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Dictionary<string, int>> GetArticleCountByCategoryAsync(CancellationToken cancellationToken = default)
        {
            return await _context.NewsArticles
                .Where(a => !string.IsNullOrEmpty(a.Category))
                .GroupBy(a => a.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Category, g => g.Count, cancellationToken);
        }

        public async Task RemoveAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            _context.NewsArticles.Remove(article);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
} 