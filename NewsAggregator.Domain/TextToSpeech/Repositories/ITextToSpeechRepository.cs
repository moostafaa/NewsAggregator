using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Domain.TextToSpeech.Repositories
{
    public interface ITextToSpeechRepository
    {
        Task<AudioConversion> GetConversionByIdAsync(Guid id);
        Task<IEnumerable<AudioConversion>> GetConversionsByStatusAsync(ConversionStatus status);
        Task<IEnumerable<VoiceConfiguration>> GetVoiceConfigurationsAsync();
        Task AddConversionAsync(AudioConversion conversion);
        Task UpdateConversionAsync(AudioConversion conversion);
        Task<VoiceConfiguration> GetDefaultVoiceConfigurationAsync(string languageCode);
        Task SaveVoiceConfigurationAsync(VoiceConfiguration config);
    }
} 