using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.Management.Entities;
using NewsAggregator.Domain.Management.Models;
using NewsAggregator.Domain.Management.ValueObjects;

namespace NewsAggregator.Domain.Management.Services
{
    public interface ICloudConfigurationService
    {
        Task<CloudConfiguration> GetConfigurationAsync();
        Task<CloudConfiguration> UpdateConfigurationAsync(
            CloudProvider provider,
            CloudCredentials credentials,
            string region,
            Dictionary<string, string> settings);
    }
} 