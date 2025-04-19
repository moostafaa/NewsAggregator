using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.TextToSpeech.Storage
{
    /// <summary>
    /// Stores audio files in Azure Blob Storage
    /// </summary>
    public class AzureBlobStorageProvider : IAudioStorageProvider
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobStorageProvider> _logger;
        private readonly AzureBlobStorageOptions _options;

        public string ProviderName => "AzureBlob";

        public AzureBlobStorageProvider(
            IOptions<AzureBlobStorageOptions> options,
            ILogger<AzureBlobStorageProvider> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize Azure Blob Storage client
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            
            // Ensure container exists
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> StoreAudioAsync(
            byte[] audioData, 
            string fileName, 
            string mimeType, 
            AudioMetadata metadata = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate unique blob name to avoid collisions
                var blobName = GenerateUniqueBlobName(fileName);
                
                // Get a reference to the blob
                var blobClient = _containerClient.GetBlobClient(blobName);
                
                // Prepare metadata dictionary
                var metadataDict = new Dictionary<string, string>();
                
                if (metadata != null)
                {
                    // Add basic metadata as blob metadata
                    metadataDict.Add("language", metadata.Language ?? "unknown");
                    metadataDict.Add("voice-name", metadata.VoiceName ?? "default");
                    metadataDict.Add("tts-provider", metadata.TtsProvider ?? "unknown");
                    metadataDict.Add("created-at", metadata.CreatedAt.ToString("o"));
                    
                    if (metadata.ArticleId.HasValue)
                    {
                        metadataDict.Add("article-id", metadata.ArticleId.Value.ToString());
                    }
                    
                    if (metadata.DurationInSeconds.HasValue)
                    {
                        metadataDict.Add("duration-seconds", metadata.DurationInSeconds.Value.ToString());
                    }
                }
                
                // Upload audio data to Azure Blob Storage
                using (var stream = new MemoryStream(audioData))
                {
                    var blobUploadOptions = new BlobUploadOptions
                    {
                        Metadata = metadataDict,
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = mimeType
                        }
                    };
                    
                    await blobClient.UploadAsync(stream, blobUploadOptions, cancellationToken);
                }
                
                // If we have full metadata, store it as a separate JSON blob
                if (metadata != null)
                {
                    var metadataJson = JsonSerializer.Serialize(metadata);
                    var metadataBlobName = $"{blobName}.metadata.json";
                    var metadataBlobClient = _containerClient.GetBlobClient(metadataBlobName);
                    
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(metadataJson)))
                    {
                        await metadataBlobClient.UploadAsync(stream, new BlobUploadOptions
                        {
                            HttpHeaders = new BlobHttpHeaders
                            {
                                ContentType = "application/json"
                            }
                        }, cancellationToken);
                    }
                }
                
                _logger.LogInformation("Stored audio file in Azure Blob Storage: {FileName}, Blob: {BlobName}", fileName, blobName);
                
                // Return the blob name as identifier for later retrieval
                return blobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio file in Azure Blob Storage: {FileName}", fileName);
                throw;
            }
        }

        public async Task<(byte[] AudioData, AudioMetadata Metadata)> RetrieveAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get a reference to the blob
                var blobClient = _containerClient.GetBlobClient(audioIdentifier);
                
                // Download the blob to memory
                using (var memoryStream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(memoryStream, cancellationToken);
                    
                    // Try to get metadata from separate blob
                    AudioMetadata metadata = null;
                    var metadataBlobName = $"{audioIdentifier}.metadata.json";
                    var metadataBlobClient = _containerClient.GetBlobClient(metadataBlobName);
                    
                    try
                    {
                        if (await metadataBlobClient.ExistsAsync(cancellationToken))
                        {
                            using (var metadataStream = new MemoryStream())
                            {
                                await metadataBlobClient.DownloadToAsync(metadataStream, cancellationToken);
                                var metadataJson = Encoding.UTF8.GetString(metadataStream.ToArray());
                                metadata = JsonSerializer.Deserialize<AudioMetadata>(metadataJson);
                            }
                        }
                        else
                        {
                            // Try to create metadata from blob metadata
                            var blobProperties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                            var blobMetadata = blobProperties.Value.Metadata;
                            
                            if (blobMetadata.Count > 0)
                            {
                                metadata = new AudioMetadata
                                {
                                    Language = blobMetadata.TryGetValue("language", out var language) ? language : null,
                                    VoiceName = blobMetadata.TryGetValue("voice-name", out var voiceName) ? voiceName : null,
                                    TtsProvider = blobMetadata.TryGetValue("tts-provider", out var ttsProvider) ? ttsProvider : null,
                                    CreatedAt = blobMetadata.TryGetValue("created-at", out var createdAt) 
                                        ? DateTime.Parse(createdAt) : DateTime.UtcNow
                                };
                                
                                if (blobMetadata.TryGetValue("article-id", out var articleId) && Guid.TryParse(articleId, out var guidValue))
                                {
                                    metadata.ArticleId = guidValue;
                                }
                                
                                if (blobMetadata.TryGetValue("duration-seconds", out var durationStr) && double.TryParse(durationStr, out var duration))
                                {
                                    metadata.DurationInSeconds = duration;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve metadata for blob: {BlobName}", audioIdentifier);
                    }
                    
                    return (memoryStream.ToArray(), metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audio file from Azure Blob Storage: {BlobName}", audioIdentifier);
                throw new FileNotFoundException($"Audio file not found: {audioIdentifier}", ex.Message);
            }
        }

        public async Task DeleteAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get a reference to the blob
                var blobClient = _containerClient.GetBlobClient(audioIdentifier);
                
                // Delete the blob
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                
                // Try to delete metadata blob if it exists
                var metadataBlobName = $"{audioIdentifier}.metadata.json";
                var metadataBlobClient = _containerClient.GetBlobClient(metadataBlobName);
                
                await metadataBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                
                _logger.LogInformation("Deleted audio file from Azure Blob Storage: {BlobName}", audioIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file from Azure Blob Storage: {BlobName}", audioIdentifier);
                throw;
            }
        }

        public async Task<(Stream AudioStream, string MimeType)> GetAudioStreamAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get a reference to the blob
                var blobClient = _containerClient.GetBlobClient(audioIdentifier);
                
                // Get blob properties to determine the content type
                var blobProperties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                var mimeType = blobProperties.Value.ContentType;
                
                // Open a download stream
                var download = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
                
                return (download, mimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming audio file from Azure Blob Storage: {BlobName}", audioIdentifier);
                throw new FileNotFoundException($"Audio file not found: {audioIdentifier}", ex.Message);
            }
        }
        
        private string GenerateUniqueBlobName(string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(fileName);
            
            // Ensure the file name is safe for blob storage
            var safeName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "-")
                .Replace(".", "-")
                .ToLowerInvariant();
            
            // Combine components to create a unique blob name
            return $"{_options.BlobPrefix}/{timestamp}-{guid}-{safeName}{extension}";
        }
    }
} 