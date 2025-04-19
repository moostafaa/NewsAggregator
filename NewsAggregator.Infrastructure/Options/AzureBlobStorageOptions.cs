namespace NewsAggregator.Infrastructure.Options
{
    public class AzureBlobStorageOptions
    {
        public const string SectionName = "Storage:AzureBlob";
        
        /// <summary>
        /// Azure Blob Storage connection string
        /// </summary>
        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        
        /// <summary>
        /// Container name for audio files
        /// </summary>
        public string ContainerName { get; set; } = "audio-files";
        
        /// <summary>
        /// Prefix for blob names (like a folder path)
        /// </summary>
        public string BlobPrefix { get; set; } = "tts";
        
        /// <summary>
        /// Whether to use Azure CDN for audio files
        /// </summary>
        public bool UseCdn { get; set; } = false;
        
        /// <summary>
        /// Azure CDN endpoint URL (if UseCdn is true)
        /// </summary>
        public string CdnEndpoint { get; set; } = string.Empty;
    }
} 