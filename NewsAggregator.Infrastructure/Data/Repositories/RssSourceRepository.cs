using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Infrastructure.Data.Repositories
{
    public class RssSourceRepository : IRssSourceRepository
    {
        private readonly NewsAggregatorDbContext _context;

        public RssSourceRepository(NewsAggregatorDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // IRepository<RssSource> implementation
        async Task<RssSource> IRepository<RssSource>.GetByIdAsync(Guid id)
        {
            return await GetByIdAsync(id);
        }

        async Task<IEnumerable<RssSource>> IRepository<RssSource>.GetAllAsync()
        {
            return await GetAllAsync();
        }

        async Task IRepository<RssSource>.AddAsync(RssSource entity)
        {
            await AddAsync(entity);
        }

        async Task IRepository<RssSource>.UpdateAsync(RssSource entity)
        {
            await UpdateAsync(entity);
        }

        async Task IRepository<RssSource>.DeleteAsync(RssSource entity)
        {
            await DeleteAsync(entity);
        }

        // IRssSourceRepository implementation
        public async Task<RssSource> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.RssSources.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<RssSource> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _context.RssSources
                .FirstOrDefaultAsync(x => x.Url == url, cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.RssSources.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.RssSources
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetByProviderTypeAsync(NewsProviderType providerType, CancellationToken cancellationToken = default)
        {
            return await _context.RssSources
                .Where(x => x.ProviderType == providerType)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetPendingFetchAsync(int count, TimeSpan threshold, CancellationToken cancellationToken = default)
        {
            var thresholdTime = DateTime.UtcNow.Subtract(threshold);
            
            return await _context.RssSources
                .Where(x => x.IsActive && x.LastFetchedAt < thresholdTime)
                .OrderBy(x => x.LastFetchedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(RssSource source, CancellationToken cancellationToken = default)
        {
            await _context.RssSources.AddAsync(source, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(RssSource source, CancellationToken cancellationToken = default)
        {
            _context.RssSources.Update(source);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(RssSource source, CancellationToken cancellationToken = default)
        {
            _context.RssSources.Remove(source);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsWithUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _context.RssSources.AnyAsync(x => x.Url == url, cancellationToken);
        }
    }
} 