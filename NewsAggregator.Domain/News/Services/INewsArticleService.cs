using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Services
{
    public interface INewsArticleService
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
        /// Gets latest articles
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetLatestArticlesAsync(int count, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles by source
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetArticlesBySourceAsync(Guid sourceId, int count, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles by category
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetArticlesByCategoryAsync(string category, int count, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Searches articles by keywords
        /// </summary>
        Task<IEnumerable<NewsArticle>> SearchArticlesAsync(string searchTerm, int count, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new article
        /// </summary>
        Task<NewsArticle> CreateAsync(
            Guid sourceId,
            string title,
            string url,
            string description,
            DateTime publishedAt,
            string author = null,
            string imageUrl = null,
            string category = null,
            string content = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates article content
        /// </summary>
        Task<bool> UpdateContentAsync(Guid id, string content, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an article
        /// </summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets articles for news feed with optional filtering
        /// </summary>
        Task<IEnumerable<NewsArticle>> GetArticlesForFeedAsync(
            int count,
            string category = null,
            Guid? sourceId = null,
            CancellationToken cancellationToken = default);
    }
} 