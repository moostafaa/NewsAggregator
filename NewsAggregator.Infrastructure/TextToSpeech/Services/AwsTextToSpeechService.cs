using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using NewsAggregator.Infrastructure.TextToSpeech.Interfaces;

namespace NewsAggregator.Infrastructure.TextToSpeech.Services
{
    public class AwsTextToSpeechService : IStorageAwareTextToSpeechProvider
    {
        private readonly ILogger<AwsTextToSpeechService> _logger;
        private readonly IAmazonPolly _pollyClient;
        private readonly string _outputPath;
        private IAudioStorageProvider _storageProvider;

        public AwsTextToSpeechService(ILogger<AwsTextToSpeechService> logger, IConfiguration configuration)
        {
            _logger = logger;
            var awsAccessKey = configuration["AWS:AccessKey"];
            var awsSecretKey = configuration["AWS:SecretKey"];
            var awsRegion = configuration["AWS:Region"];
            _outputPath = configuration["Storage:AudioOutputPath"];

            if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey))
                throw new ArgumentException("AWS credentials are not configured");

            if (string.IsNullOrEmpty(awsRegion))
                throw new ArgumentException("AWS region is not configured");

            if (string.IsNullOrEmpty(_outputPath))
                throw new ArgumentException("Audio output path is not configured");

            var region = RegionEndpoint.GetBySystemName(awsRegion);
            _pollyClient = new AmazonPollyClient(awsAccessKey, awsSecretKey, region);
        }
        
        public void SetStorageProvider(IAudioStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _logger.LogInformation("Set storage provider for AWS TTS service: {ProviderName}", storageProvider.ProviderName);
        }
        
        public IAudioStorageProvider GetStorageProvider()
        {
            return _storageProvider;
        }

        public async Task<(string StoragePath, int DurationMs)> ConvertToSpeechAsync(
            string text, 
            VoiceConfiguration voiceConfig)
        {
            try
            {
                _logger.LogInformation("Converting text to speech using AWS Polly with voice {VoiceId}", voiceConfig.VoiceId);

                var request = new SynthesizeSpeechRequest
                {
                    Text = text,
                    VoiceId = voiceConfig.VoiceId,
                    OutputFormat = OutputFormat.Mp3,
                    Engine = Engine.Neural // Use neural engine for better quality
                };

                var response = await _pollyClient.SynthesizeSpeechAsync(request);
                
                byte[] audioData;
                using (var memoryStream = new MemoryStream())
                {
                    await response.AudioStream.CopyToAsync(memoryStream);
                    audioData = memoryStream.ToArray();
                }
                
                // Calculate duration using NAudio
                int durationMs;
                using (var ms = new MemoryStream(audioData))
                using (var mp3Reader = new Mp3FileReader(ms))
                {
                    durationMs = (int)mp3Reader.TotalTime.TotalMilliseconds;
                }
                
                // Store in the configured storage provider if available
                string storagePath;
                if (_storageProvider != null)
                {
                    // Create metadata
                    var metadata = new AudioMetadata
                    {
                        OriginalText = text,
                        Language = voiceConfig.LanguageCode,
                        VoiceName = voiceConfig.VoiceId,
                        TtsProvider = "AWS",
                        DurationInSeconds = durationMs / 1000.0
                    };
                    
                    // Store with the provider
                    var fileName = $"{Guid.NewGuid()}.mp3";
                    storagePath = await _storageProvider.StoreAudioAsync(
                        audioData, 
                        fileName, 
                        "audio/mpeg", 
                        metadata);
                    
                    _logger.LogInformation("Stored audio using provider {ProviderName}, path: {Path}", 
                        _storageProvider.ProviderName, storagePath);
                }
                else
                {
                    // Fall back to file system
                    var fileName = $"aws_{voiceConfig.VoiceId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}.mp3";
                    var localPath = Path.Combine(_outputPath, fileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(_outputPath);
                    
                    // Save to file
                    await File.WriteAllBytesAsync(localPath, audioData);
                    storagePath = fileName;
                    
                    _logger.LogInformation("Stored audio in file system (legacy mode): {Path}", localPath);
                }

                return (storagePath, durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech with AWS Polly");
                throw;
            }
        }

        public async Task<bool> ValidateVoiceConfigurationAsync(VoiceConfiguration voiceConfig)
        {
            try
            {
                var response = await _pollyClient.DescribeVoicesAsync(new DescribeVoicesRequest
                {
                    LanguageCode = voiceConfig.LanguageCode
                });

                return response.Voices.Any(v => v.Id.Value == voiceConfig.VoiceId);
            }
            catch
            {
                return false;
            }
        }
    }
} 