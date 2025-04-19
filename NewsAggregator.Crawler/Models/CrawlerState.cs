using System;

namespace NewsAggregator.Crawler.Models
{
    /// <summary>
    /// Represents the state of a crawler operation
    /// </summary>
    public class CrawlerState
    {
        /// <summary>
        /// Unique identifier for the crawler state
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Identifier for the crawler instance
        /// </summary>
        public string CrawlerId { get; set; }
        
        /// <summary>
        /// Current status of the crawler
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// When the crawler operation started
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the crawler operation completed (if applicable)
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Number of sources processed
        /// </summary>
        public int SourcesProcessed { get; set; }
        
        /// <summary>
        /// Number of articles processed
        /// </summary>
        public int ArticlesProcessed { get; set; }
        
        /// <summary>
        /// Number of sources that failed to process
        /// </summary>
        public int SourcesFailed { get; set; }
        
        /// <summary>
        /// Any error messages from the crawler operation
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Creates a new crawler state with the specified ID
        /// </summary>
        /// <param name="crawlerId">The ID of the crawler instance</param>
        /// <returns>A new CrawlerState instance</returns>
        public static CrawlerState Create(string crawlerId)
        {
            return new CrawlerState
            {
                CrawlerId = crawlerId,
                Status = "Starting",
                StartedAt = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Marks the crawler operation as completed
        /// </summary>
        /// <param name="sourcesProcessed">Number of sources processed</param>
        /// <param name="articlesProcessed">Number of articles processed</param>
        public void MarkCompleted(int sourcesProcessed, int articlesProcessed)
        {
            Status = "Completed";
            CompletedAt = DateTime.UtcNow;
            SourcesProcessed = sourcesProcessed;
            ArticlesProcessed = articlesProcessed;
        }
        
        /// <summary>
        /// Marks the crawler operation as failed
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public void MarkFailed(string errorMessage)
        {
            Status = "Failed";
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
        }
    }
} 