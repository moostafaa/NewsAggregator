using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.News.Services
{
    public interface INewsCrawlerService
    {
        /// <summary>
        /// Fetches articles from a specific source
        /// </summary>
        /// <param name="source">The news source to fetch from</param>
        /// <param name="maxArticles">Maximum number of articles to fetch (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of news articles</returns>
        Task<IEnumerable<NewsArticle>> FetchArticlesFromSourceAsync(
            NewsSource source, 
            int maxArticles = 20, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fetches articles from all configured sources
        /// </summary>
        /// <param name="maxArticlesPerSource">Maximum number of articles to fetch per source (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of news articles from all sources</returns>
        Task<IEnumerable<NewsArticle>> FetchArticlesFromAllSourcesAsync(
            int maxArticlesPerSource = 10, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds a new source to be crawled
        /// </summary>
        /// <param name="source">The news source to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddSourceAsync(NewsSource source, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes a source from the crawler
        /// </summary>
        /// <param name="sourceUrl">The URL of the news source to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveSourceAsync(string sourceUrl, CancellationToken cancellationToken = default);
    }
} 