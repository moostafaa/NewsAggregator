using System;

namespace NewsAggregator.Crawler.Options
{
    public class DistributedCrawlerOptions
    {
        public const string SectionName = "DistributedCrawler";
        
        /// <summary>
        /// Whether distributed crawling is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Name of this crawler server (used for coordination)
        /// </summary>
        public string ServerName { get; set; } = $"Crawler-{Environment.MachineName}";
        
        /// <summary>
        /// Coordination mode: Redis, Database, or Local
        /// </summary>
        public string CoordinationMode { get; set; } = "Redis";
        
        /// <summary>
        /// Batch size for processing sources
        /// </summary>
        public int BatchSize { get; set; } = 5;
        
        /// <summary>
        /// Number of worker threads to use per crawler instance
        /// </summary>
        public int WorkerThreads { get; set; } = 4;
        
        /// <summary>
        /// API endpoint of the main NewsAggregator service
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://localhost:5001";
        
        /// <summary>
        /// Authentication key for the API (if required)
        /// </summary>
        public string ApiKey { get; set; } = "";
    }
} 