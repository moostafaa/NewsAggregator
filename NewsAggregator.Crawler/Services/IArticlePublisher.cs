using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Interface for publishing crawled articles back to the main application
    /// </summary>
    public interface IArticlePublisher
    {
        /// <summary>
        /// Publishes a single article to the main application
        /// </summary>
        /// <param name="article">The article to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if publication was successful</returns>
        Task<bool> PublishArticleAsync(NewsArticle article, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Publishes a batch of articles to the main application
        /// </summary>
        /// <param name="articles">The articles to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of successfully published articles</returns>
        Task<int> PublishArticlesAsync(IEnumerable<NewsArticle> articles, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Reports crawler statistics to the main application
        /// </summary>
        /// <param name="crawlerName">Name of the crawler instance</param>
        /// <param name="sourcesProcessed">Number of sources processed</param>
        /// <param name="articlesPublished">Number of articles published</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ReportCrawlerStatsAsync(
            string crawlerName, 
            int sourcesProcessed, 
            int articlesPublished, 
            CancellationToken cancellationToken = default);
    }
} 