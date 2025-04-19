using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Infrastructure.TextToSpeech.Services
{
    public class GoogleTextToSpeechService : ITextToSpeechProvider
    {
        private readonly ILogger<GoogleTextToSpeechService> _logger;
        private readonly TextToSpeechClient _textToSpeechClient;
        private readonly string _outputPath;

        public GoogleTextToSpeechService(
            ILogger<GoogleTextToSpeechService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _outputPath = configuration["Storage:AudioOutputPath"];

            if (string.IsNullOrEmpty(_outputPath))
                throw new ArgumentException("Audio output path is not configured");

            // Create a credential using a JSON file or Google Cloud environment settings
            string credentialsPath = configuration["Google:CredentialsPath"];
            if (!string.IsNullOrEmpty(credentialsPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            }

            _textToSpeechClient = TextToSpeechClient.Create();
        }

        public async Task<(string StoragePath, int DurationMs)> ConvertToSpeechAsync(
            string text, 
            VoiceConfiguration voiceConfig)
        {
            try
            {
                _logger.LogInformation("Converting text to speech using Google TTS with voice {VoiceId}", voiceConfig.VoiceId);

                // Configure the speech parameters
                var input = new SynthesisInput { Text = text };
                var voice = new VoiceSelectionParams
                {
                    LanguageCode = voiceConfig.LanguageCode,
                    Name = voiceConfig.VoiceId,
                    SsmlGender = ParseGender(voiceConfig.Gender)
                };
                var audioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3,
                    SpeakingRate = voiceConfig.SpeakingRate,
                    Pitch = voiceConfig.Pitch
                };

                // Send the synthesis request
                var response = await _textToSpeechClient.SynthesizeSpeechAsync(input, voice, audioConfig);

                // Save the audio to a file
                string fileName = $"{Guid.NewGuid()}.mp3";
                string filePath = Path.Combine(_outputPath, fileName);

                await File.WriteAllBytesAsync(filePath, response.AudioContent.ToByteArray());

                // Calculate audio duration using NAudio
                int durationMs = CalculateAudioDuration(filePath);

                return (filePath, durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Google TTS conversion");
                throw;
            }
        }

        private SsmlVoiceGender ParseGender(string gender)
        {
            return gender?.ToLowerInvariant() switch
            {
                "male" => SsmlVoiceGender.Male,
                "female" => SsmlVoiceGender.Female,
                _ => SsmlVoiceGender.Neutral
            };
        }

        private int CalculateAudioDuration(string filePath)
        {
            try
            {
                using var reader = new Mp3FileReader(filePath);
                return (int)reader.TotalTime.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to calculate audio duration");
                return 0;
            }
        }
    }
} 