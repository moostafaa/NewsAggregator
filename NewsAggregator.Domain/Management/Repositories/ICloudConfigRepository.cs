using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.Management.Entities;

namespace NewsAggregator.Domain.Management.Repositories
{
    public interface ICloudConfigRepository : IRepository<CloudConfig>
    {
        Task<CloudConfig> GetByProviderAsync(CloudProvider provider);
    }
} 