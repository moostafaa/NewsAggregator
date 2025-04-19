using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Services
{
    public interface IRssSourceService
    {
        /// <summary>
        /// Gets a source by ID
        /// </summary>
        Task<RssSource> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a source by URL
        /// </summary>
        Task<RssSource> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all sources
        /// </summary>
        Task<IEnumerable<RssSource>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets active sources
        /// </summary>
        Task<IEnumerable<RssSource>> GetActiveAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets sources by provider type
        /// </summary>
        Task<IEnumerable<RssSource>> GetByProviderTypeAsync(NewsProviderType providerType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets sources ready for fetching
        /// </summary>
        Task<IEnumerable<RssSource>> GetPendingFetchAsync(int count, TimeSpan threshold, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new source
        /// </summary>
        Task<RssSource> CreateAsync(string name, string url, string description, NewsProviderType providerType, string defaultCategory = "general", CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing source
        /// </summary>
        Task<RssSource> UpdateAsync(Guid id, string name, string url, string description, NewsProviderType providerType, string defaultCategory = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Activates a source
        /// </summary>
        Task<RssSource> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deactivates a source
        /// </summary>
        Task<RssSource> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a source
        /// </summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the last fetched time for a source
        /// </summary>
        Task UpdateLastFetchedTimeAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates a URL to ensure it's a valid RSS feed
        /// </summary>
        Task<bool> ValidateRssUrlAsync(string url, CancellationToken cancellationToken = default);
    }
} 