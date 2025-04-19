using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Interface for fetching news sources from the main application
    /// </summary>
    public interface ISourceService
    {
        /// <summary>
        /// Gets all news sources from the main application
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of news sources</returns>
        Task<IEnumerable<NewsSource>> GetAllSourcesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets sources that match the provided criteria
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="providerType">Optional provider type filter</param>
        /// <param name="limit">Maximum number of sources to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Filtered list of news sources</returns>
        Task<IEnumerable<NewsSource>> GetSourcesByFilterAsync(
            string category = null, 
            string providerType = null, 
            int limit = 100, 
            CancellationToken cancellationToken = default);
    }
} 