namespace NewsAggregator.Infrastructure.Options
{
    public class SqlServerStorageOptions
    {
        public const string SectionName = "Storage:SqlServer";
        
        /// <summary>
        /// Maximum audio file size in bytes (default: 50MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 52428800; // 50MB
        
        /// <summary>
        /// Whether to optimize for read performance by storing frequently accessed data in a cache
        /// </summary>
        public bool UseCache { get; set; } = true;
        
        /// <summary>
        /// Number of days to keep audio files before automatic cleanup
        /// </summary>
        public int RetentionDays { get; set; } = 30;
    }
} 