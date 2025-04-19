using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.Management.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.Management.Repositories
{
    public interface INewsProviderRepository : IRepository<NewsProvider>
    {
        Task<NewsProvider> GetByNameAsync(string name);
        Task<IEnumerable<NewsProvider>> GetByProviderTypeAsync(NewsProviderType providerType);
        Task<IEnumerable<NewsProvider>> GetActiveAsync();
        Task<bool> ExistsWithNameAsync(string name);
    }
} 