using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.TextToSpeech.Services
{
    public class TextToSpeechFactory : ITextToSpeechFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TextToSpeechFactory> _logger;
        private readonly IAudioStorageFactory _audioStorageFactory;
        private readonly TextToSpeechOptions _options;

        public TextToSpeechFactory(
            IServiceProvider serviceProvider,
            IAudioStorageFactory audioStorageFactory,
            IOptions<TextToSpeechOptions> options,
            ILogger<TextToSpeechFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _audioStorageFactory = audioStorageFactory ?? throw new ArgumentNullException(nameof(audioStorageFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ITextToSpeechProvider GetProvider(string providerName)
        {
            try
            {
                ITextToSpeechProvider provider;
                
                switch (providerName?.ToLowerInvariant())
                {
                    case "aws":
                        provider = _serviceProvider.GetRequiredService<AwsTextToSpeechService>();
                        break;
                    case "azure":
                        provider = _serviceProvider.GetRequiredService<AzureTextToSpeechService>();
                        break;
                    case "google":
                        provider = _serviceProvider.GetRequiredService<GoogleTextToSpeechService>();
                        break;
                    case null:
                        throw new ArgumentException("Provider name cannot be null");
                    case "":
                        throw new ArgumentException("Provider name cannot be empty");
                    default:
                        throw new ArgumentException($"Unsupported TTS provider: {providerName}");
                }
                
                // Set the storage provider based on configuration
                string storageProviderName = _options.DefaultStorageProvider;
                
                try
                {
                    IAudioStorageProvider storageProvider;
                    
                    if (string.IsNullOrWhiteSpace(storageProviderName))
                    {
                        storageProvider = _audioStorageFactory.GetDefaultProvider();
                        _logger.LogInformation("Using default audio storage provider: {ProviderName}", storageProvider.ProviderName);
                    }
                    else
                    {
                        storageProvider = _audioStorageFactory.GetProvider(storageProviderName);
                        _logger.LogInformation("Using configured audio storage provider: {ProviderName}", storageProvider.ProviderName);
                    }
                    
                    // If the provider implements IStorageAwareTextToSpeechProvider, set the storage provider
                    if (provider is IStorageAwareTextToSpeechProvider storageAwareProvider)
                    {
                        storageAwareProvider.SetStorageProvider(storageProvider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set storage provider for TTS provider {ProviderName}", providerName);
                }
                
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TTS provider {ProviderName}", providerName);
                throw;
            }
        }

        public ITextToSpeechProvider GetProviderForVoiceConfig(VoiceConfiguration voiceConfig)
        {
            if (voiceConfig == null)
                throw new ArgumentNullException(nameof(voiceConfig));

            return GetProvider(voiceConfig.Provider);
        }
        
        public ITextToSpeechProvider GetProviderWithStorage(string providerName, string storageProviderName)
        {
            try
            {
                // Get the TTS provider
                var ttsProvider = GetProvider(providerName);
                
                // Get the storage provider
                var storageProvider = _audioStorageFactory.GetProvider(storageProviderName);
                
                // If the provider implements IStorageAwareTextToSpeechProvider, set the storage provider
                if (ttsProvider is IStorageAwareTextToSpeechProvider storageAwareProvider)
                {
                    storageAwareProvider.SetStorageProvider(storageProvider);
                    _logger.LogInformation("Using specific audio storage provider: {StorageProviderName} for TTS provider: {TtsProviderName}", 
                        storageProvider.ProviderName, providerName);
                }
                
                return ttsProvider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TTS provider {ProviderName} with storage provider {StorageProviderName}", 
                    providerName, storageProviderName);
                throw;
            }
        }
    }

    public interface ITextToSpeechFactory
    {
        ITextToSpeechProvider GetProvider(string providerName);
        ITextToSpeechProvider GetProviderForVoiceConfig(VoiceConfiguration voiceConfig);
        ITextToSpeechProvider GetProviderWithStorage(string providerName, string storageProviderName);
    }
    
    /// <summary>
    /// Interface for TTS providers that are aware of storage providers
    /// </summary>
    public interface IStorageAwareTextToSpeechProvider : ITextToSpeechProvider
    {
        /// <summary>
        /// Sets the storage provider to use for this TTS provider
        /// </summary>
        void SetStorageProvider(IAudioStorageProvider storageProvider);
        
        /// <summary>
        /// Gets the current storage provider
        /// </summary>
        IAudioStorageProvider GetStorageProvider();
    }
} 