using System.Collections.Generic;

namespace NewsAggregator.Domain.TextToSpeech.Services
{
    /// <summary>
    /// Factory for creating audio storage providers
    /// </summary>
    public interface IAudioStorageFactory
    {
        /// <summary>
        /// Gets the default audio storage provider
        /// </summary>
        IAudioStorageProvider GetDefaultProvider();
        
        /// <summary>
        /// Gets an audio storage provider by name
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        IAudioStorageProvider GetProvider(string providerName);
        
        /// <summary>
        /// Gets all available storage providers
        /// </summary>
        IEnumerable<IAudioStorageProvider> GetAllProviders();
        
        /// <summary>
        /// Gets the names of all available storage providers
        /// </summary>
        IEnumerable<string> GetAvailableProviderNames();
    }
} 