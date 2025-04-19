using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NewsAggregator.Domain.TextToSpeech.Services
{
    /// <summary>
    /// Interface for audio storage providers
    /// </summary>
    public interface IAudioStorageProvider
    {
        /// <summary>
        /// Gets the name of the storage provider
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Stores audio data and returns the identifier/URL to retrieve it
        /// </summary>
        /// <param name="audioData">The binary audio data</param>
        /// <param name="fileName">Suggested file name</param>
        /// <param name="mimeType">MIME type of the audio</param>
        /// <param name="metadata">Optional metadata for the audio file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>URI/identifier to access the stored audio</returns>
        Task<string> StoreAudioAsync(
            byte[] audioData, 
            string fileName, 
            string mimeType, 
            AudioMetadata metadata = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves audio data by its identifier
        /// </summary>
        /// <param name="audioIdentifier">The identifier/URL of the audio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Audio data and metadata</returns>
        Task<(byte[] AudioData, AudioMetadata Metadata)> RetrieveAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes audio data by its identifier
        /// </summary>
        /// <param name="audioIdentifier">The identifier/URL of the audio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a stream to the audio for direct streaming
        /// </summary>
        /// <param name="audioIdentifier">The identifier/URL of the audio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream of audio data and its MIME type</returns>
        Task<(Stream AudioStream, string MimeType)> GetAudioStreamAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Metadata for audio files
    /// </summary>
    public class AudioMetadata
    {
        /// <summary>
        /// Original text that was converted to speech
        /// </summary>
        public string OriginalText { get; set; }
        
        /// <summary>
        /// Language of the audio
        /// </summary>
        public string Language { get; set; }
        
        /// <summary>
        /// Voice name or identifier used
        /// </summary>
        public string VoiceName { get; set; }
        
        /// <summary>
        /// TTS provider that generated the audio
        /// </summary>
        public string TtsProvider { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Duration of the audio in seconds
        /// </summary>
        public double? DurationInSeconds { get; set; }
        
        /// <summary>
        /// Article ID if the audio is for a news article
        /// </summary>
        public Guid? ArticleId { get; set; }
        
        /// <summary>
        /// Additional custom properties
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }
} 