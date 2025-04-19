using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Repositories
{
    public interface IRssSourceRepository : IRepository<RssSource>
    {
        Task<RssSource> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<RssSource> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
        Task<IEnumerable<RssSource>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<RssSource>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<RssSource>> GetByProviderTypeAsync(NewsProviderType providerType, CancellationToken cancellationToken = default);
        Task<IEnumerable<RssSource>> GetPendingFetchAsync(int count, TimeSpan threshold, CancellationToken cancellationToken = default);
        Task AddAsync(RssSource source, CancellationToken cancellationToken = default);
        Task UpdateAsync(RssSource source, CancellationToken cancellationToken = default);
        Task DeleteAsync(RssSource source, CancellationToken cancellationToken = default);
        Task<bool> ExistsWithUrlAsync(string url, CancellationToken cancellationToken = default);
    }
} 