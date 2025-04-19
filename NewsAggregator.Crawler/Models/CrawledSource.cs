using System;
using System.Collections.Generic;

namespace NewsAggregator.Crawler.Models
{
    /// <summary>
    /// Represents a news source that is crawled by the microservice
    /// </summary>
    public class CrawledSource
    {
        /// <summary>
        /// Unique identifier for the source
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the source
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// URL of the source
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Optional description of the source
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The categories this source is associated with
        /// </summary>
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        
        /// <summary>
        /// When this source was last crawled
        /// </summary>
        public DateTime? LastCrawledAt { get; set; }
        
        /// <summary>
        /// Number of articles found in the last crawl
        /// </summary>
        public int LastArticleCount { get; set; }
        
        /// <summary>
        /// Whether the last crawl was successful
        /// </summary>
        public bool LastCrawlSuccessful { get; set; }
        
        /// <summary>
        /// Any error message from the last crawl
        /// </summary>
        public string LastCrawlError { get; set; }
        
        /// <summary>
        /// When this source was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this source was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Creates a new source from a domain NewsSource value object
        /// </summary>
        /// <param name="newsSource">The domain NewsSource value object</param>
        /// <returns>A new CrawledSource instance</returns>
        public static CrawledSource FromDomain(NewsAggregator.Domain.News.ValueObjects.NewsSource newsSource)
        {
            var source = new CrawledSource
            {
                Name = newsSource.Name,
                Url = newsSource.Url.ToString(),
                LastCrawledAt = null
            };
            
            return source;
        }
    }
} 