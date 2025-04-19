using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.Management.Models;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.Management.Services
{
    public interface INewsProviderConfigService
    {
        Task<IEnumerable<ProviderConfig>> GetAllConfigsAsync();
        Task<ProviderConfig> GetConfigAsync(NewsProviderType providerType);
        Task<ProviderConfig> UpdateConfigAsync(
            NewsProviderType providerType,
            string apiKey,
            string baseUrl,
            Dictionary<string, string> additionalSettings);
    }
} 