using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.TextToSpeech.ValueObjects
{
    public class VoiceConfiguration : ValueObject
    {
        public string VoiceId { get; private set; }
        public string LanguageCode { get; private set; }
        public string Gender { get; private set; }
        public string Provider { get; private set; }  // "Azure", "AWS", "Google"
        public float SpeakingRate { get; private set; }
        public float Pitch { get; private set; }

        private VoiceConfiguration(
            string voiceId, 
            string languageCode, 
            string gender, 
            string provider,
            float speakingRate = 1.0f,
            float pitch = 1.0f)
        {
            VoiceId = voiceId;
            LanguageCode = languageCode;
            Gender = gender;
            Provider = provider;
            SpeakingRate = speakingRate;
            Pitch = pitch;
        }

        public static VoiceConfiguration Create(
            string voiceId,
            string languageCode,
            string gender,
            string provider,
            float? speakingRate = null,
            float? pitch = null)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
                throw new DomainException("Voice ID cannot be empty");

            if (string.IsNullOrWhiteSpace(languageCode))
                throw new DomainException("Language code cannot be empty");

            if (string.IsNullOrWhiteSpace(provider))
                throw new DomainException("Provider cannot be empty");

            if (provider != "Azure" && provider != "AWS" && provider != "Google")
                throw new DomainException("Provider must be either 'Azure', 'AWS', or 'Google'");

            return new VoiceConfiguration(
                voiceId,
                languageCode,
                gender ?? "Neutral",
                provider,
                speakingRate ?? 1.0f,
                pitch ?? 1.0f);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return VoiceId;
            yield return LanguageCode;
            yield return Gender;
            yield return Provider;
            yield return SpeakingRate;
            yield return Pitch;
        }
    }
} 