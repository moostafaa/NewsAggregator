using NewsAggregator.Domain.TextToSpeech.Services;

namespace NewsAggregator.Infrastructure.TextToSpeech.Interfaces
{
    /// <summary>
    /// Interface for text-to-speech providers that work with storage providers
    /// </summary>
    public interface IStorageAwareTextToSpeechProvider : ITextToSpeechProvider
    {
        /// <summary>
        /// Sets the storage provider for this text-to-speech provider
        /// </summary>
        /// <param name="storageProvider">The storage provider to use</param>
        void SetStorageProvider(IAudioStorageProvider storageProvider);

        /// <summary>
        /// Gets the current storage provider for this text-to-speech provider
        /// </summary>
        /// <returns>The current storage provider</returns>
        IAudioStorageProvider GetStorageProvider();
    }
} 