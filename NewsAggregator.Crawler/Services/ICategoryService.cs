using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Crawler.Models;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Interface for services that manage news categories
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Refreshes the local database with categories from the main application
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of categories updated</returns>
        Task<int> RefreshCategoriesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Classifies an article into a category
        /// </summary>
        /// <param name="title">Article title</param>
        /// <param name="content">Article content</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="sourceCategory">Original source category</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The category for the article</returns>
        Task<Category> ClassifyArticleAsync(
            string title, 
            string content, 
            string sourceName = null, 
            string sourceCategory = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive categories</param>
        /// <param name="providerType">Optional provider type filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of categories</returns>
        Task<IEnumerable<Category>> GetCategoriesAsync(
            bool includeInactive = false, 
            string providerType = null, 
            CancellationToken cancellationToken = default);
    }
} 