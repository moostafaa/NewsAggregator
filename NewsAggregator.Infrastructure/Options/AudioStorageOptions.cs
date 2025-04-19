namespace NewsAggregator.Infrastructure.Options
{
    public class AudioStorageOptions
    {
        public const string SectionName = "Storage:Audio";
        
        /// <summary>
        /// The default storage provider to use
        /// </summary>
        public string DefaultProvider { get; set; } = "FileSystem";
        
        /// <summary>
        /// Whether to enable the File System storage provider
        /// </summary>
        public bool EnableFileSystem { get; set; } = true;
        
        /// <summary>
        /// Whether to enable the SQL Server storage provider
        /// </summary>
        public bool EnableSqlServer { get; set; } = false;
        
        /// <summary>
        /// Whether to enable the MinIO storage provider
        /// </summary>
        public bool EnableMinio { get; set; } = false;
        
        /// <summary>
        /// Whether to enable the Azure Blob storage provider
        /// </summary>
        public bool EnableAzureBlob { get; set; } = false;
    }
} 