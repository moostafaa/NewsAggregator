using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.Options;
using NewsAggregator.Infrastructure.TextToSpeech.Persistence;

namespace NewsAggregator.Infrastructure.TextToSpeech.Storage
{
    /// <summary>
    /// Stores audio files in SQL Server database
    /// </summary>
    public class SqlServerAudioStorageProvider : IAudioStorageProvider
    {
        private readonly TextToSpeechDbContext _dbContext;
        private readonly ILogger<SqlServerAudioStorageProvider> _logger;
        private readonly SqlServerStorageOptions _options;

        public string ProviderName => "SqlServer";

        public SqlServerAudioStorageProvider(
            TextToSpeechDbContext dbContext,
            IOptions<SqlServerStorageOptions> options,
            ILogger<SqlServerAudioStorageProvider> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                // Create unique identifier
                var audioId = Guid.NewGuid();
                
                // Prepare metadata JSON if available
                string metadataJson = null;
                if (metadata != null)
                {
                    metadataJson = JsonSerializer.Serialize(metadata);
                }
                
                // Create the audio entity
                var audioEntity = new AudioEntity
                {
                    Id = audioId,
                    FileName = fileName,
                    MimeType = mimeType,
                    AudioData = audioData,
                    Metadata = metadataJson,
                    CreatedAt = DateTime.UtcNow,
                    FileSize = audioData.Length,
                    OriginalText = metadata?.OriginalText,
                    ArticleId = metadata?.ArticleId
                };
                
                // Save to database
                _dbContext.AudioFiles.Add(audioEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Stored audio file in database: {FileName}, ID: {Id}", fileName, audioId);
                
                // Return the ID as string for later retrieval
                return audioId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio file in database: {FileName}", fileName);
                throw;
            }
        }

        public async Task<(byte[] AudioData, AudioMetadata Metadata)> RetrieveAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(audioIdentifier, out var audioId))
                {
                    throw new ArgumentException("Invalid audio identifier format", nameof(audioIdentifier));
                }
                
                // Retrieve from database
                var audioEntity = await _dbContext.AudioFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == audioId, cancellationToken);
                
                if (audioEntity == null)
                {
                    _logger.LogWarning("Audio file not found in database: {AudioId}", audioId);
                    throw new FileNotFoundException($"Audio file not found with ID: {audioId}");
                }
                
                // Parse metadata if available
                AudioMetadata metadata = null;
                if (!string.IsNullOrEmpty(audioEntity.Metadata))
                {
                    metadata = JsonSerializer.Deserialize<AudioMetadata>(audioEntity.Metadata);
                }
                
                return (audioEntity.AudioData, metadata);
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is ArgumentException))
            {
                _logger.LogError(ex, "Error retrieving audio file from database: {AudioId}", audioIdentifier);
                throw;
            }
        }

        public async Task DeleteAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(audioIdentifier, out var audioId))
                {
                    throw new ArgumentException("Invalid audio identifier format", nameof(audioIdentifier));
                }
                
                // Find the entity
                var audioEntity = await _dbContext.AudioFiles
                    .FirstOrDefaultAsync(a => a.Id == audioId, cancellationToken);
                
                if (audioEntity == null)
                {
                    _logger.LogWarning("Audio file not found in database for deletion: {AudioId}", audioId);
                    return; // Nothing to delete
                }
                
                // Remove from database
                _dbContext.AudioFiles.Remove(audioEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Deleted audio file from database: {AudioId}", audioId);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                _logger.LogError(ex, "Error deleting audio file from database: {AudioId}", audioIdentifier);
                throw;
            }
        }

        public async Task<(Stream AudioStream, string MimeType)> GetAudioStreamAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(audioIdentifier, out var audioId))
                {
                    throw new ArgumentException("Invalid audio identifier format", nameof(audioIdentifier));
                }
                
                // Retrieve from database (only get the necessary fields)
                var audioInfo = await _dbContext.AudioFiles
                    .AsNoTracking()
                    .Select(a => new { a.Id, a.AudioData, a.MimeType })
                    .FirstOrDefaultAsync(a => a.Id == audioId, cancellationToken);
                
                if (audioInfo == null)
                {
                    _logger.LogWarning("Audio file not found in database for streaming: {AudioId}", audioId);
                    throw new FileNotFoundException($"Audio file not found with ID: {audioId}");
                }
                
                // Create a memory stream from the byte array
                var stream = new MemoryStream(audioInfo.AudioData);
                
                // Return the stream and MIME type
                return (stream, audioInfo.MimeType);
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is ArgumentException))
            {
                _logger.LogError(ex, "Error streaming audio file from database: {AudioId}", audioIdentifier);
                throw;
            }
        }
    }
} 