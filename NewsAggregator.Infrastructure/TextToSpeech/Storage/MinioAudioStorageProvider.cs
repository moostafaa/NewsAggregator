using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.TextToSpeech.Storage
{
    /// <summary>
    /// Stores audio files in MinIO (S3-compatible) object storage
    /// </summary>
    public class MinioAudioStorageProvider : IAudioStorageProvider
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioAudioStorageProvider> _logger;
        private readonly MinioStorageOptions _options;

        public string ProviderName => "MinIO";

        public MinioAudioStorageProvider(
            IOptions<MinioStorageOptions> options,
            ILogger<MinioAudioStorageProvider> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize MinIO client
            _minioClient = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithRegion(_options.Region);
                
            if (_options.UseSSL)
            {
                _minioClient = _minioClient.WithSSL();
            }
            
            // Build the client
            _minioClient = _minioClient.Build();
            
            // Ensure bucket exists
            EnsureBucketExistsAsync().GetAwaiter().GetResult();
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
                // Generate unique object name to avoid collisions
                var objectName = GenerateUniqueObjectName(fileName);
                
                // Store metadata as object tags
                var metadataDict = new Dictionary<string, string>();
                if (metadata != null)
                {
                    // Store basic metadata as object tags
                    metadataDict.Add("original-text-length", metadata.OriginalText?.Length.ToString() ?? "0");
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
                
                // Add content type as metadata
                metadataDict.Add("Content-Type", mimeType);
                
                // Upload audio data to MinIO
                using (var stream = new MemoryStream(audioData))
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(objectName)
                        .WithStreamData(stream)
                        .WithObjectSize(audioData.Length)
                        .WithContentType(mimeType)
                        .WithHeaders(metadataDict);
                        
                    await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
                }
                
                // If we have full metadata, store it as a separate JSON object
                if (metadata != null)
                {
                    var metadataJson = JsonSerializer.Serialize(metadata);
                    var metadataObjectName = $"{objectName}.metadata.json";
                    
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(metadataJson)))
                    {
                        var putMetadataArgs = new PutObjectArgs()
                            .WithBucket(_options.BucketName)
                            .WithObject(metadataObjectName)
                            .WithStreamData(stream)
                            .WithObjectSize(stream.Length)
                            .WithContentType("application/json");
                            
                        await _minioClient.PutObjectAsync(putMetadataArgs, cancellationToken);
                    }
                }
                
                _logger.LogInformation("Stored audio file in MinIO: {FileName}, Object: {ObjectName}", fileName, objectName);
                
                // Return the object name as identifier for later retrieval
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio file in MinIO: {FileName}", fileName);
                throw;
            }
        }

        public async Task<(byte[] AudioData, AudioMetadata Metadata)> RetrieveAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get audio data from MinIO
                using (var memoryStream = new MemoryStream())
                {
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(audioIdentifier)
                        .WithCallbackStream(stream => stream.CopyTo(memoryStream));
                        
                    await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
                    
                    // Try to get metadata from separate JSON file
                    AudioMetadata metadata = null;
                    var metadataObjectName = $"{audioIdentifier}.metadata.json";
                    
                    try
                    {
                        using (var metadataStream = new MemoryStream())
                        {
                            var getMetadataArgs = new GetObjectArgs()
                                .WithBucket(_options.BucketName)
                                .WithObject(metadataObjectName)
                                .WithCallbackStream(stream => stream.CopyTo(metadataStream));
                                
                            await _minioClient.GetObjectAsync(getMetadataArgs, cancellationToken);
                            
                            var metadataJson = Encoding.UTF8.GetString(metadataStream.ToArray());
                            metadata = JsonSerializer.Deserialize<AudioMetadata>(metadataJson);
                        }
                    }
                    catch (ObjectNotFoundException)
                    {
                        // Metadata file doesn't exist, that's OK
                        _logger.LogInformation("Metadata file not found for object: {ObjectName}", metadataObjectName);
                    }
                    
                    return (memoryStream.ToArray(), metadata);
                }
            }
            catch (ObjectNotFoundException)
            {
                _logger.LogWarning("Audio file not found in MinIO: {AudioIdentifier}", audioIdentifier);
                throw new FileNotFoundException($"Audio file not found: {audioIdentifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audio file from MinIO: {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }

        public async Task DeleteAudioAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Delete the audio object
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(audioIdentifier);
                    
                await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
                
                // Try to delete the metadata object if it exists
                try
                {
                    var metadataObjectName = $"{audioIdentifier}.metadata.json";
                    var removeMetadataArgs = new RemoveObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(metadataObjectName);
                        
                    await _minioClient.RemoveObjectAsync(removeMetadataArgs, cancellationToken);
                }
                catch (ObjectNotFoundException)
                {
                    // Metadata object doesn't exist, that's OK
                }
                
                _logger.LogInformation("Deleted audio file from MinIO: {AudioIdentifier}", audioIdentifier);
            }
            catch (ObjectNotFoundException)
            {
                _logger.LogWarning("Audio file not found in MinIO for deletion: {AudioIdentifier}", audioIdentifier);
                // Not throwing an exception as the file doesn't exist anyway
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file from MinIO: {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }

        public async Task<(Stream AudioStream, string MimeType)> GetAudioStreamAsync(
            string audioIdentifier, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // First, check if the object exists and get its stats to determine the content type
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(audioIdentifier);
                
                var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
                var mimeType = objectStat.ContentType;
                
                // Create a proxy stream that will download from MinIO on demand
                var stream = new MinioObjectStream(_minioClient, _options.BucketName, audioIdentifier);
                
                return (stream, mimeType);
            }
            catch (ObjectNotFoundException)
            {
                _logger.LogWarning("Audio file not found in MinIO for streaming: {AudioIdentifier}", audioIdentifier);
                throw new FileNotFoundException($"Audio file not found: {audioIdentifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming audio file from MinIO: {AudioIdentifier}", audioIdentifier);
                throw;
            }
        }
        
        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
                bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
                
                if (!bucketExists)
                {
                    var makeBucketArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                    _logger.LogInformation("Created MinIO bucket: {BucketName}", _options.BucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring MinIO bucket exists: {BucketName}", _options.BucketName);
                throw;
            }
        }
        
        private string GenerateUniqueObjectName(string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(fileName);
            
            // Ensure the file name is safe for S3 object storage
            var safeName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "-")
                .Replace(".", "-")
                .ToLowerInvariant();
            
            // Combine components to create a unique object name
            return $"{_options.ObjectPrefix}/{timestamp}-{guid}-{safeName}{extension}";
        }
    }
    
    /// <summary>
    /// Custom stream implementation that lazily fetches data from MinIO
    /// </summary>
    public class MinioObjectStream : Stream
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly string _objectName;
        private Stream _internalStream;
        
        public MinioObjectStream(IMinioClient minioClient, string bucketName, string objectName)
        {
            _minioClient = minioClient;
            _bucketName = bucketName;
            _objectName = objectName;
            _internalStream = new MemoryStream();
        }
        
        private async Task EnsureStreamInitializedAsync()
        {
            if (_internalStream.Length == 0)
            {
                var memoryStream = new MemoryStream();
                
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(_objectName)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream));
                    
                await _minioClient.GetObjectAsync(getObjectArgs);
                
                memoryStream.Position = 0;
                _internalStream = memoryStream;
            }
        }
        
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        
        public override long Length
        {
            get
            {
                EnsureStreamInitializedAsync().GetAwaiter().GetResult();
                return _internalStream.Length;
            }
        }
        
        public override long Position
        {
            get => _internalStream.Position;
            set => _internalStream.Position = value;
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureStreamInitializedAsync().GetAwaiter().GetResult();
            return _internalStream.Read(buffer, offset, count);
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureStreamInitializedAsync().GetAwaiter().GetResult();
            return _internalStream.Seek(offset, origin);
        }
        
        public override void Flush() => _internalStream.Flush();
        
        public override void SetLength(long value) => throw new NotSupportedException();
        
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _internalStream?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
} 