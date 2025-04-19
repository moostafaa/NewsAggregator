using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.Repositories;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;

namespace NewsAggregator.Application.TextToSpeech
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly ITextToSpeechRepository _repository;
        private readonly Dictionary<string, ITextToSpeechProvider> _providers;
        private readonly ILogger<TextToSpeechService> _logger;

        public TextToSpeechService(
            ITextToSpeechRepository repository,
            IEnumerable<(string ProviderName, ITextToSpeechProvider Provider)> providers,
            ILogger<TextToSpeechService> logger)
        {
            _repository = repository;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p.Provider);
            _logger = logger;
        }

        public async Task<Guid> ConvertToSpeechAsync(string text, string languageCode, string provider = null)
        {
            try
            {
                var voiceConfig = await GetVoiceConfigurationAsync(languageCode, provider);
                if (voiceConfig == null)
                {
                    throw new ApplicationException($"No voice configuration found for language {languageCode}");
                }

                var conversion = AudioConversion.Create(text, voiceConfig);
                await _repository.AddConversionAsync(conversion);

                // Start async conversion process
                _ = ProcessConversionAsync(conversion);

                return conversion.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech");
                throw;
            }
        }

        public async Task<AudioConversion> GetConversionStatusAsync(Guid conversionId)
        {
            return await _repository.GetConversionByIdAsync(conversionId);
        }

        public async Task<IEnumerable<VoiceConfiguration>> GetAvailableVoicesAsync(string languageCode = null)
        {
            var configurations = await _repository.GetVoiceConfigurationsAsync();
            if (!string.IsNullOrEmpty(languageCode))
            {
                return configurations.Where(c => c.LanguageCode == languageCode);
            }
            return configurations;
        }

        private async Task<VoiceConfiguration> GetVoiceConfigurationAsync(string languageCode, string provider)
        {
            if (!string.IsNullOrEmpty(provider))
            {
                var configs = await _repository.GetVoiceConfigurationsAsync();
                return configs.FirstOrDefault(c => 
                    c.LanguageCode == languageCode && 
                    c.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
            }

            return await _repository.GetDefaultVoiceConfigurationAsync(languageCode);
        }

        private async Task ProcessConversionAsync(AudioConversion conversion)
        {
            try
            {
                if (!_providers.TryGetValue(conversion.VoiceConfig.Provider, out var provider))
                {   
                    throw new ApplicationException($"Provider {conversion.VoiceConfig.Provider} not found");
                }

                var (storagePath, durationMs) = await provider.ConvertToSpeechAsync(
                    conversion.Text,
                    conversion.VoiceConfig);

                conversion.SetCompleted(storagePath, durationMs);
                await _repository.UpdateConversionAsync(conversion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing conversion {ConversionId}", conversion.Id);
                conversion.SetFailed(ex.Message);
                await _repository.UpdateConversionAsync(conversion);
            }
        }
    }
} 