using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.TextToSpeech.Services
{
    /// <summary>
    /// Factory for creating audio storage providers based on configuration
    /// </summary>
    public class AudioStorageFactory : IAudioStorageFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AudioStorageOptions _options;
        private readonly ILogger<AudioStorageFactory> _logger;
        private readonly Dictionary<string, IAudioStorageProvider> _providers;

        public AudioStorageFactory(
            IServiceProvider serviceProvider,
            IOptions<AudioStorageOptions> options,
            ILogger<AudioStorageFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize providers dictionary
            _providers = new Dictionary<string, IAudioStorageProvider>(StringComparer.OrdinalIgnoreCase);
            
            // Load all available providers
            LoadAvailableProviders();
        }

        public IAudioStorageProvider GetDefaultProvider()
        {
            // Get the provider specified as default in configuration
            if (string.IsNullOrWhiteSpace(_options.DefaultProvider))
            {
                _logger.LogWarning("No default storage provider specified in configuration. Using first available provider.");
                return _providers.Values.FirstOrDefault() ?? 
                    throw new InvalidOperationException("No storage providers are available.");
            }
            
            return GetProvider(_options.DefaultProvider);
        }

        public IAudioStorageProvider GetProvider(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
                
            if (_providers.TryGetValue(providerName, out var provider))
                return provider;
                
            throw new ArgumentException($"Storage provider '{providerName}' is not available. " +
                $"Available providers: {string.Join(", ", _providers.Keys)}");
        }

        public IEnumerable<IAudioStorageProvider> GetAllProviders()
        {
            return _providers.Values;
        }

        public IEnumerable<string> GetAvailableProviderNames()
        {
            return _providers.Keys;
        }
        
        private void LoadAvailableProviders()
        {
            try
            {
                // Try to load FileSystem provider if enabled
                if (_options.EnableFileSystem)
                {
                    try
                    {
                        var fileSystemProvider = _serviceProvider.GetRequiredService<NewsAggregator.Infrastructure.TextToSpeech.Storage.FileSystemAudioStorageProvider>();
                        _providers.Add(fileSystemProvider.ProviderName, fileSystemProvider);
                        _logger.LogInformation("Loaded FileSystem storage provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load FileSystem storage provider");
                    }
                }
                
                // Try to load SQL Server provider if enabled
                if (_options.EnableSqlServer)
                {
                    try
                    {
                        var sqlServerProvider = _serviceProvider.GetRequiredService<NewsAggregator.Infrastructure.TextToSpeech.Storage.SqlServerAudioStorageProvider>();
                        _providers.Add(sqlServerProvider.ProviderName, sqlServerProvider);
                        _logger.LogInformation("Loaded SQL Server storage provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load SQL Server storage provider");
                    }
                }
                
                // Try to load MinIO provider if enabled
                if (_options.EnableMinio)
                {
                    try
                    {
                        var minioProvider = _serviceProvider.GetRequiredService<NewsAggregator.Infrastructure.TextToSpeech.Storage.MinioAudioStorageProvider>();
                        _providers.Add(minioProvider.ProviderName, minioProvider);
                        _logger.LogInformation("Loaded MinIO storage provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load MinIO storage provider");
                    }
                }
                
                // Try to load Azure Blob provider if enabled
                if (_options.EnableAzureBlob)
                {
                    try
                    {
                        var azureBlobProvider = _serviceProvider.GetRequiredService<NewsAggregator.Infrastructure.TextToSpeech.Storage.AzureBlobStorageProvider>();
                        _providers.Add(azureBlobProvider.ProviderName, azureBlobProvider);
                        _logger.LogInformation("Loaded Azure Blob storage provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load Azure Blob storage provider");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing audio storage providers");
            }
            
            if (_providers.Count == 0)
            {
                _logger.LogWarning("No audio storage providers were loaded. Audio storage functionality will not be available.");
            }
            else
            {
                _logger.LogInformation("Loaded {Count} audio storage providers: {Providers}", 
                    _providers.Count, string.Join(", ", _providers.Keys));
            }
        }
    }
} 