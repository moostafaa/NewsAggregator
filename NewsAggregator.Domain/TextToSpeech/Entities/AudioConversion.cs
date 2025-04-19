using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.ValueObjects;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Domain.TextToSpeech.Entities
{
    public class AudioConversion : Entity
    {
        public string Text { get; private set; }
        public VoiceConfiguration VoiceConfig { get; private set; }
        public string AudioFormat { get; private set; }
        public int DurationMs { get; private set; }
        public string StoragePath { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public ConversionStatus Status { get; private set; }
        public string ErrorMessage { get; private set; }

        private AudioConversion(
            Guid id,
            string text,
            VoiceConfiguration voiceConfig,
            string audioFormat) : base(id)
        {
            Text = text;
            VoiceConfig = voiceConfig;
            AudioFormat = audioFormat;
            CreatedAt = DateTime.UtcNow;
            Status = ConversionStatus.Pending;
        }

        public static AudioConversion Create(
            string text,
            VoiceConfiguration voiceConfig,
            string audioFormat = "mp3")
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new DomainException("Text cannot be empty");

            if (voiceConfig == null)
                throw new DomainException("Voice configuration cannot be null");

            return new AudioConversion(Guid.NewGuid(), text, voiceConfig, audioFormat);
        }

        public void SetCompleted(string storagePath, int durationMs)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new DomainException("Storage path cannot be empty");

            if (durationMs <= 0)
                throw new DomainException("Duration must be greater than zero");

            StoragePath = storagePath;
            DurationMs = durationMs;
            Status = ConversionStatus.Completed;
            ErrorMessage = null;
        }

        public void SetFailed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new DomainException("Error message cannot be empty");

            Status = ConversionStatus.Failed;
            ErrorMessage = errorMessage;
        }
    }

    public enum ConversionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
} 