using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Repositories
{
    public interface INewsArticleRepository : IRepository<NewsArticle>
    {
        /// <summary>
        /// Gets an article by ID
        /// </summary>
        Task<NewsArticle> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets an article by URL
        /// </summary>
        Task<NewsArticle> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles by category
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetByCategoryAsync(string category, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles by tag
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetByTagAsync(string tag, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles by source
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetBySourceAsync(string sourceName, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets latest articles
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetLatestAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets saved articles
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetSavedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Searches articles by keywords
        /// </summary>
        Task<IEnumerable<NewsArticle>> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds an article
        /// </summary>
        Task AddAsync(NewsArticle article, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an article
        /// </summary>
        Task UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes articles older than specified days
        /// </summary>
        Task RemoveOlderThanAsync(int days, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles count by category
        /// </summary>
        Task<Dictionary<string, int>> GetArticleCountByCategoryAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes a specific article
        /// </summary>
        Task RemoveAsync(NewsArticle article, CancellationToken cancellationToken = default);
    }
} 