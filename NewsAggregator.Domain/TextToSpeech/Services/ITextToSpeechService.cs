using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Domain.TextToSpeech.Services
{
    public interface ITextToSpeechService
    {
        Task<Guid> ConvertToSpeechAsync(string text, string languageCode, string provider = null);
        Task<AudioConversion> GetConversionStatusAsync(Guid conversionId);
        Task<IEnumerable<VoiceConfiguration>> GetAvailableVoicesAsync(string languageCode = null);
    }
} 