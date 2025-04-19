using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Crawler.Coordination
{
    /// <summary>
    /// Interface for coordinating work between multiple crawler instances
    /// </summary>
    public interface IWorkCoordinator
    {
        /// <summary>
        /// Initialize the coordinator
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Acquire a batch of sources to process
        /// </summary>
        /// <param name="batchSize">The number of sources to acquire</param>
        /// <param name="workerId">The ID of the worker requesting sources</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of sources to process</returns>
        Task<IEnumerable<NewsSource>> AcquireSourcesAsync(int batchSize, string workerId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Report completion of processing for a source
        /// </summary>
        /// <param name="source">The source that was processed</param>
        /// <param name="articlesCount">Number of articles processed</param>
        /// <param name="workerId">The ID of the worker reporting completion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ReportSourceCompletionAsync(NewsSource source, int articlesCount, string workerId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if all sources have been processed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all sources have been processed</returns>
        Task<bool> IsWorkCompleteAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Reset the coordinator for a new crawl cycle
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }
} 