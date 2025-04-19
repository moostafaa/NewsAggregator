using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.TextToSpeech.Storage
{
    /// <summary>
    /// Stores audio files on the local file system
    /// </summary>
    public class FileSystemAudioStorageProvider : IAudioStorageProvider
    {
        private readonly ILogger<FileSystemAudioStorageProvider> _logger;
        private readonly FileSystemStorageOptions _options;

        public string ProviderName => "FileSystem";

        public FileSystemAudioStorageProvider(
            IOptions<FileSystemStorageOptions> options,
            ILogger<FileSystemAudioStorageProvider> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Ensure directory exists
            if (!Directory.Exists(_options.StoragePath))
            {
                Directory.CreateDirectory(_options.StoragePath);
                _logger.LogInformation("Created audio storage directory: {Path}", _options.StoragePath);
            }
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
                // Generate unique file name to avoid collisions
                var safeFileName = SanitizeFileName(fileName);
                var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}_{safeFileName}";
                var filePath = Path.Combine(_options.StoragePath, uniqueFileName);
                
                // Store the audio data
                await File.WriteAllBytesAsync(filePath, audioData, cancellationToken);
                
                // Store metadata in a companion file if metadata is provided
                if (metadata != null)
                {
                    var metadataPath = $"{filePath}.metadata.json";
                    var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);
                }
                
                _logger.LogInformation("Stored audio file: {FileName}", uniqueFileName);
                
                // Return the relative identifier (file name) that can be used to retrieve the file later
                return uniqueFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio file {FileName}", fileName);
                throw;
            }
        }

        public async Task<(byte[] AudioData, AudioMetadata Metadata)> RetrieveAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_options.StoragePath, audioIdentifier);
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Audio file not found: {AudioIdentifier}", audioIdentifier);
                    throw new FileNotFoundException($"Audio file not found: {audioIdentifier}");
                }
                
                // Read audio data
                var audioData = await File.ReadAllBytesAsync(filePath, cancellationToken);
                
                // Try to read metadata from companion file
                var metadataPath = $"{filePath}.metadata.json";
                AudioMetadata metadata = null;
                
                if (File.Exists(metadataPath))
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                    metadata = JsonSerializer.Deserialize<AudioMetadata>(metadataJson);
                }
                
                return (audioData, metadata);
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                _logger.LogError(ex, "Error retrieving audio file {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }

        public Task DeleteAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_options.StoragePath, audioIdentifier);
                var metadataPath = $"{filePath}.metadata.json";
                
                // Delete audio file if exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                // Delete metadata file if exists
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }
                
                _logger.LogInformation("Deleted audio file: {AudioIdentifier}", audioIdentifier);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }

        public async Task<(Stream AudioStream, string MimeType)> GetAudioStreamAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = Path.Combine(_options.StoragePath, audioIdentifier);
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Audio file not found: {AudioIdentifier}", audioIdentifier);
                    throw new FileNotFoundException($"Audio file not found: {audioIdentifier}");
                }
                
                // Open file stream
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                
                // Determine MIME type from extension
                var mimeType = GetMimeTypeFromFileName(audioIdentifier);
                
                return (stream, mimeType);
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                _logger.LogError(ex, "Error streaming audio file {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }
        
        private string SanitizeFileName(string fileName)
        {
            // Replace invalid characters with underscore
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Ensure filename is not too long
            const int maxLength = 100;
            if (safeName.Length > maxLength)
            {
                var extension = Path.GetExtension(safeName);
                safeName = safeName.Substring(0, maxLength - extension.Length) + extension;
            }
            
            return safeName;
        }
        
        private string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            return extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".flac" => "audio/flac",
                _ => "application/octet-stream"
            };
        }
    }
} 