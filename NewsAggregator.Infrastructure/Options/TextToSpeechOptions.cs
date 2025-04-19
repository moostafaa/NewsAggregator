using System;

namespace NewsAggregator.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for text-to-speech services
    /// </summary>
    public class TextToSpeechOptions
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "TextToSpeech";

        /// <summary>
        /// Default TTS provider to use if not specified
        /// </summary>
        public string DefaultProvider { get; set; } = "Azure";

        /// <summary>
        /// Default audio storage provider to use for TTS services
        /// </summary>
        public string DefaultStorageProvider { get; set; } = "FileSystem";

        /// <summary>
        /// Whether to cache generated audio files
        /// </summary>
        public bool CacheAudio { get; set; } = true;

        /// <summary>
        /// The maximum age of cached audio files in days before they are considered stale
        /// </summary>
        public int MaxCacheAgeDays { get; set; } = 30;
    }
} 