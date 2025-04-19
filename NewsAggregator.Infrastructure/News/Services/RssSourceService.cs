using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class RssSourceService : IRssSourceService
    {
        private readonly IRssSourceRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RssSourceService> _logger;

        public RssSourceService(
            IRssSourceRepository repository,
            HttpClient httpClient,
            ILogger<RssSourceService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RssSource> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<RssSource> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByUrlAsync(url, cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _repository.GetActiveAsync(cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetByProviderTypeAsync(NewsProviderType providerType, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByProviderTypeAsync(providerType, cancellationToken);
        }

        public async Task<IEnumerable<RssSource>> GetPendingFetchAsync(int count, TimeSpan threshold, CancellationToken cancellationToken = default)
        {
            return await _repository.GetPendingFetchAsync(count, threshold, cancellationToken);
        }

        public async Task<RssSource> CreateAsync(string name, string url, string description, NewsProviderType providerType, string defaultCategory = "general", CancellationToken cancellationToken = default)
        {
            // Validate the URL first
            if (!await ValidateRssUrlAsync(url, cancellationToken))
            {
                throw new ArgumentException("The URL does not point to a valid RSS feed", nameof(url));
            }

            // Check if the source already exists
            var exists = await _repository.ExistsWithUrlAsync(url, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"RSS source with URL '{url}' already exists");
            }

            var source = RssSource.Create(name, url, description, providerType, defaultCategory);
            await _repository.AddAsync(source, cancellationToken);
            
            _logger.LogInformation("Created RSS source {Name} with URL {Url}", name, url);
            
            return source;
        }

        public async Task<RssSource> UpdateAsync(Guid id, string name, string url, string description, NewsProviderType providerType, string defaultCategory = null, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetByIdAsync(id, cancellationToken);
            if (source == null)
            {
                throw new InvalidOperationException($"RSS source with ID '{id}' not found");
            }

            // If the URL changed, validate it
            if (url != source.Url && !await ValidateRssUrlAsync(url, cancellationToken))
            {
                throw new ArgumentException("The URL does not point to a valid RSS feed", nameof(url));
            }

            source.Update(name, url, description, providerType, defaultCategory);
            await _repository.UpdateAsync(source, cancellationToken);
            
            _logger.LogInformation("Updated RSS source {Id} with new name {Name}", id, name);
            
            return source;
        }

        public async Task<RssSource> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetByIdAsync(id, cancellationToken);
            if (source == null)
            {
                throw new InvalidOperationException($"RSS source with ID '{id}' not found");
            }

            source.Activate();
            await _repository.UpdateAsync(source, cancellationToken);
            
            _logger.LogInformation("Activated RSS source {Id}", id);
            
            return source;
        }

        public async Task<RssSource> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetByIdAsync(id, cancellationToken);
            if (source == null)
            {
                throw new InvalidOperationException($"RSS source with ID '{id}' not found");
            }

            source.Deactivate();
            await _repository.UpdateAsync(source, cancellationToken);
            
            _logger.LogInformation("Deactivated RSS source {Id}", id);
            
            return source;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetByIdAsync(id, cancellationToken);
            if (source == null)
            {
                return false;
            }

            await _repository.DeleteAsync(source, cancellationToken);
            
            _logger.LogInformation("Deleted RSS source {Id}", id);
            
            return true;
        }

        public async Task UpdateLastFetchedTimeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetByIdAsync(id, cancellationToken);
            if (source == null)
            {
                throw new InvalidOperationException($"RSS source with ID '{id}' not found");
            }

            source.UpdateLastFetchedTime();
            await _repository.UpdateAsync(source, cancellationToken);
            
            _logger.LogDebug("Updated last fetched time for RSS source {Id}", id);
        }

        public async Task<bool> ValidateRssUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var doc = XDocument.Parse(response);
                
                // Check for RSS or Atom feed structure
                var root = doc.Root;
                if (root == null)
                {
                    return false;
                }
                
                var ns = root.GetDefaultNamespace();
                
                // RSS feed typically has <rss> root and <channel> elements
                bool isRss = root.Name.LocalName.Equals("rss", StringComparison.OrdinalIgnoreCase) ||
                            doc.Descendants(ns + "channel").Any();
                
                // Atom feed typically has <feed> root
                bool isAtom = root.Name.LocalName.Equals("feed", StringComparison.OrdinalIgnoreCase);
                
                return isRss || isAtom;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate RSS URL {Url}", url);
                return false;
            }
        }
    }
} 