using System;

namespace NewsAggregator.Infrastructure.TextToSpeech.Persistence
{
    /// <summary>
    /// Entity for storing audio files in the database
    /// </summary>
    public class AudioEntity
    {
        /// <summary>
        /// Unique identifier for the audio file
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// MIME type of the audio file
        /// </summary>
        public string MimeType { get; set; }
        
        /// <summary>
        /// Binary audio data
        /// </summary>
        public byte[] AudioData { get; set; }
        
        /// <summary>
        /// Serialized metadata as JSON
        /// </summary>
        public string Metadata { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Original text used to generate the audio
        /// </summary>
        public string OriginalText { get; set; }
        
        /// <summary>
        /// Associated article ID (if applicable)
        /// </summary>
        public Guid? ArticleId { get; set; }
    }
} 