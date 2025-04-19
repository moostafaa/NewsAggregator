namespace NewsAggregator.Infrastructure.Options
{
    public class FileSystemStorageOptions
    {
        public const string SectionName = "Storage:FileSystem";
        
        /// <summary>
        /// Path where audio files will be stored
        /// </summary>
        public string StoragePath { get; set; } = "Storage/Audio";
    }
} 