using System.Threading.Tasks;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Domain.TextToSpeech.Services
{
    public interface ITextToSpeechProvider
    {
        Task<(string StoragePath, int DurationMs)> ConvertToSpeechAsync(string text, VoiceConfiguration voiceConfig);
    }
} 