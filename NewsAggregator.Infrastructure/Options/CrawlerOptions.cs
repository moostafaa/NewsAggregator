namespace NewsAggregator.Infrastructure.Options
{
    public class CrawlerOptions
    {
        public const string SectionName = "Crawler";
        
        /// <summary>
        /// Interval in minutes between crawler runs
        /// </summary>
        public int IntervalMinutes { get; set; } = 60;
        
        /// <summary>
        /// Maximum number of articles to fetch per source
        /// </summary>
        public int MaxArticlesPerSource { get; set; } = 10;
        
        /// <summary>
        /// Whether to fetch the full article content or just use the summary
        /// </summary>
        public bool FetchFullContent { get; set; } = true;
        
        /// <summary>
        /// Maximum age in days of articles to keep
        /// </summary>
        public int MaxArticleAgeDays { get; set; } = 30;
        
        /// <summary>
        /// Maximum number of sources to process concurrently
        /// If set to 0 or negative, will use the number of processor cores
        /// </summary>
        public int MaxConcurrentSources { get; set; } = 4;
    }
} 