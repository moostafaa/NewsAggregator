namespace NewsAggregator.Infrastructure.Options
{
    public class MinioStorageOptions
    {
        public const string SectionName = "Storage:MinIO";
        
        /// <summary>
        /// MinIO server endpoint
        /// </summary>
        public string Endpoint { get; set; } = "localhost:9000";
        
        /// <summary>
        /// Access key for MinIO
        /// </summary>
        public string AccessKey { get; set; } = "minioadmin";
        
        /// <summary>
        /// Secret key for MinIO
        /// </summary>
        public string SecretKey { get; set; } = "minioadmin";
        
        /// <summary>
        /// Whether to use SSL for MinIO connections
        /// </summary>
        public bool UseSSL { get; set; } = false;
        
        /// <summary>
        /// Region for MinIO (can be empty for MinIO standalone)
        /// </summary>
        public string Region { get; set; } = "";
        
        /// <summary>
        /// Bucket name for audio storage
        /// </summary>
        public string BucketName { get; set; } = "audio-files";
        
        /// <summary>
        /// Prefix for object names (like a folder)
        /// </summary>
        public string ObjectPrefix { get; set; } = "tts";
    }
} 