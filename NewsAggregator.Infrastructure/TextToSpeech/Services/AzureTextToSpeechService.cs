using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using System.Linq;
using NewsAggregator.Infrastructure.TextToSpeech.Interfaces;

namespace NewsAggregator.Infrastructure.TextToSpeech.Services
{
    public class AzureTextToSpeechService : IStorageAwareTextToSpeechProvider
    {
        private readonly ILogger<AzureTextToSpeechService> _logger;
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly string _outputPath;
        private IAudioStorageProvider _storageProvider;

        public AzureTextToSpeechService(
            ILogger<AzureTextToSpeechService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _subscriptionKey = configuration["Azure:CognitiveServices:SubscriptionKey"];
            _region = configuration["Azure:CognitiveServices:Region"];
            _outputPath = configuration["Storage:AudioOutputPath"];

            if (string.IsNullOrEmpty(_subscriptionKey))
                throw new ArgumentException("Azure Cognitive Services subscription key is not configured");

            if (string.IsNullOrEmpty(_region))
                throw new ArgumentException("Azure Cognitive Services region is not configured");

            if (string.IsNullOrEmpty(_outputPath))
                throw new ArgumentException("Audio output path is not configured");
        }

        public void SetStorageProvider(IAudioStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _logger.LogInformation("Set storage provider for Azure TTS service: {ProviderName}", storageProvider.ProviderName);
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
                _logger.LogInformation("Converting text to speech using Azure TTS with voice {VoiceId}", voiceConfig.VoiceId);

                var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
                config.SpeechSynthesisVoiceName = voiceConfig.VoiceId;
                
                // Create a memory stream to hold the audio data
                using var audioOutputStream = AudioOutputStream.CreatePullStream();
                using var audioConfig = AudioConfig.FromStreamOutput(audioOutputStream);
                using var synthesizer = new SpeechSynthesizer(config, audioConfig);

                // Add SSML for rate and pitch control
                string ssml = $@"
                    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{voiceConfig.LanguageCode}'>
                        <voice name='{voiceConfig.VoiceId}'>
                            <prosody rate='{voiceConfig.SpeakingRate}' pitch='{voiceConfig.Pitch}st'>
                                {text}
                            </prosody>
                        </voice>
                    </speak>";

                var result = await synthesizer.SpeakSsmlAsync(ssml);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    // Read the audio data from the stream
                    byte[] audioData;
                    using (var memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        uint bytesRead = 0;
                        while ((bytesRead = audioOutputStream.Read(buffer)) > 0)
                        {
                            memoryStream.Write(buffer, 0, (int)bytesRead);
                        }
                        audioData = memoryStream.ToArray();
                    }
                    
                    // Calculate duration (convert to MP3 temporarily for duration calculation)
                    int durationMs;
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new WaveFileWriter(memoryStream, new WaveFormat(16000, 16, 1)))
                    {
                        writer.Write(audioData, 0, audioData.Length);
                        writer.Flush();
                        memoryStream.Position = 0;
                        
                        using (var reader = new WaveFileReader(memoryStream))
                        {
                            durationMs = (int)reader.TotalTime.TotalMilliseconds;
                        }
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
                            TtsProvider = "Azure",
                            DurationInSeconds = durationMs / 1000.0
                        };
                        
                        // Store with the provider
                        var fileName = $"{Guid.NewGuid()}.wav";
                        storagePath = await _storageProvider.StoreAudioAsync(
                            audioData, 
                            fileName, 
                            "audio/wav", 
                            metadata);
                        
                        _logger.LogInformation("Stored audio using provider {ProviderName}, path: {Path}", 
                            _storageProvider.ProviderName, storagePath);
                    }
                    else
                    {
                        // Fall back to file system
                        var fileName = $"azure_{voiceConfig.VoiceId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}.wav";
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
                else
                {
                    _logger.LogError("Speech synthesis failed: {Reason}, {ErrorDetails}", 
                        result.Reason, result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonErrorDetails));
                    throw new Exception($"Speech synthesis failed: {result.Reason}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech with Azure TTS");
                throw;
            }
        }

        public async Task<bool> ValidateVoiceConfigurationAsync(VoiceConfiguration voiceConfig)
        {
            // Basic validation for now - just check if required fields are provided
            if (string.IsNullOrEmpty(voiceConfig.VoiceId))
                return false;

            if (string.IsNullOrEmpty(voiceConfig.LanguageCode))
                return false;

            // In a real application, you would validate against available Azure voices
            // by calling the Azure Cognitive Services API
            return true;
        }
    }
} 