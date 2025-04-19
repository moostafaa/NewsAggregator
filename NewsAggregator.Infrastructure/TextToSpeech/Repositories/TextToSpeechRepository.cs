using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.Repositories;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using NewsAggregator.Infrastructure.TextToSpeech.Persistence;

namespace NewsAggregator.Infrastructure.TextToSpeech.Repositories
{
    public class TextToSpeechRepository : ITextToSpeechRepository
    {
        private readonly TextToSpeechDbContext _context;

        public TextToSpeechRepository(TextToSpeechDbContext context)
        {
            _context = context;
        }

        public async Task<AudioConversion> GetConversionByIdAsync(Guid id)
        {
            return await _context.AudioConversions.FindAsync(id);
        }

        public async Task<IEnumerable<AudioConversion>> GetConversionsByStatusAsync(ConversionStatus status)
        {
            return await _context.AudioConversions
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<VoiceConfiguration>> GetVoiceConfigurationsAsync()
        {
            var configurations = await _context.VoiceConfigurations.ToListAsync();
            return configurations.Select(c => c.ToDomainModel());
        }

        public async Task AddConversionAsync(AudioConversion conversion)
        {
            await _context.AudioConversions.AddAsync(conversion);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateConversionAsync(AudioConversion conversion)
        {
            _context.AudioConversions.Update(conversion);
            await _context.SaveChangesAsync();
        }

        public async Task<VoiceConfiguration> GetDefaultVoiceConfigurationAsync(string languageCode)
        {
            var config = await _context.VoiceConfigurations
                .FirstOrDefaultAsync(v => v.LanguageCode == languageCode && v.IsDefault);

            return config?.ToDomainModel();
        }

        public async Task SaveVoiceConfigurationAsync(VoiceConfiguration config)
        {
            var data = VoiceConfigurationData.FromDomainModel(config);

            // If this is the first configuration for this language and provider, make it default
            var existingConfig = await _context.VoiceConfigurations
                .AnyAsync(v => v.LanguageCode == config.LanguageCode && v.Provider == config.Provider);

            if (!existingConfig)
            {
                data.IsDefault = true;
            }

            await _context.VoiceConfigurations.AddAsync(data);
            await _context.SaveChangesAsync();
        }
    }
} 