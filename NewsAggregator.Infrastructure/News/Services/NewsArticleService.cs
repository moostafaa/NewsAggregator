using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class NewsArticleService : INewsArticleService
    {
        private readonly INewsArticleRepository _newsArticleRepository;
        private readonly IRssSourceRepository _rssSourceRepository;
        private readonly ILogger<NewsArticleService> _logger;

        public NewsArticleService(
            INewsArticleRepository newsArticleRepository,
            IRssSourceRepository rssSourceRepository,
            ILogger<NewsArticleService> logger)
        {
            _newsArticleRepository = newsArticleRepository ?? throw new ArgumentNullException(nameof(newsArticleRepository));
            _rssSourceRepository = rssSourceRepository ?? throw new ArgumentNullException(nameof(rssSourceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NewsArticle> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var article = await _newsArticleRepository.GetByIdAsync(id, cancellationToken);
            if (article == null)
            {
                _logger.LogWarning("Article with id {Id} not found", id);
            }
            return article;
        }

        public async Task<NewsArticle> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _newsArticleRepository.GetByUrlAsync(url, cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestArticlesAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _newsArticleRepository.GetLatestAsync(1, count, cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetArticlesBySourceAsync(Guid sourceId, int count, CancellationToken cancellationToken = default)
        {
            var source = await _rssSourceRepository.GetByIdAsync(sourceId, cancellationToken);
            if (source == null)
            {
                _logger.LogWarning("RSS source with id {Id} not found when fetching articles", sourceId);
                return Enumerable.Empty<NewsArticle>();
            }

            return await _newsArticleRepository.GetBySourceAsync(source.Name, 1, count, cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> GetArticlesByCategoryAsync(string category, int count, CancellationToken cancellationToken = default)
        {
            return await _newsArticleRepository.GetByCategoryAsync(category, 1, count, cancellationToken);
        }

        public async Task<IEnumerable<NewsArticle>> SearchArticlesAsync(string searchTerm, int count, CancellationToken cancellationToken = default)
        {
            return await _newsArticleRepository.SearchAsync(searchTerm, 1, count, cancellationToken);
        }

        public async Task<NewsArticle> CreateAsync(
            Guid sourceId,
            string title,
            string url,
            string description,
            DateTime publishedAt,
            string author = null,
            string imageUrl = null,
            string category = null,
            string content = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Article title cannot be empty", nameof(title));
            
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Article URL cannot be empty", nameof(url));

            var existingArticle = await _newsArticleRepository.GetByUrlAsync(url, cancellationToken);
            if (existingArticle != null)
            {
                _logger.LogInformation("Article with URL {Url} already exists, skipping creation", url);
                return existingArticle;
            }

            var source = await _rssSourceRepository.GetByIdAsync(sourceId, cancellationToken);
            if (source == null)
            {
                _logger.LogError("Cannot create article: RSS source with id {Id} not found", sourceId);
                throw new InvalidOperationException($"RSS source with id {sourceId} not found");
            }

            // Use source's default category if none provided
            if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(source.DefaultCategory))
            {
                category = source.DefaultCategory;
            }

            // Create a NewsSource value object from the RssSource entity
            var newsSource = NewsSource.Create(source.Id, source.Name, source.ProviderType.ToString());
            
            // Create the article using the domain model's static factory method
            var article = NewsArticle.Create(
                title,
                description,  // Use description as summary
                content ?? string.Empty,  // Use content as body, or empty string if null
                newsSource,
                publishedAt,
                category ?? "uncategorized",
                url
            );

            await _newsArticleRepository.AddAsync(article, cancellationToken);
            _logger.LogInformation("Created new article with id {Id} from source {SourceId}", article.Id, sourceId);

            return article;
        }

        public async Task<bool> UpdateContentAsync(Guid id, string content, CancellationToken cancellationToken = default)
        {
            var article = await _newsArticleRepository.GetByIdAsync(id, cancellationToken);
            if (article == null)
            {
                _logger.LogWarning("Cannot update content: Article with id {Id} not found", id);
                return false;
            }

            // Use the domain model's method to update content
            article.UpdateContent(article.Title, article.Summary, content);
            
            await _newsArticleRepository.UpdateAsync(article, cancellationToken);
            _logger.LogInformation("Updated content for article with id {Id}", id);
            
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var article = await _newsArticleRepository.GetByIdAsync(id, cancellationToken);
            if (article == null)
            {
                _logger.LogWarning("Cannot delete: Article with id {Id} not found", id);
                return false;
            }

            // Use the new RemoveAsync method to delete a specific article
            await _newsArticleRepository.RemoveAsync(article, cancellationToken);
            _logger.LogInformation("Deleted article with id {Id}", id);
            
            return true;
        }

        public async Task<IEnumerable<NewsArticle>> GetArticlesForFeedAsync(
            int count,
            string category = null,
            Guid? sourceId = null,
            CancellationToken cancellationToken = default)
        {
            // This method was removed from the repository, so we need to implement it here
            IEnumerable<NewsArticle> articles;
            
            if (!string.IsNullOrEmpty(category))
            {
                articles = await _newsArticleRepository.GetByCategoryAsync(category, 1, count, cancellationToken);
            }
            else if (sourceId.HasValue)
            {
                var source = await _rssSourceRepository.GetByIdAsync(sourceId.Value, cancellationToken);
                if (source == null)
                {
                    return Enumerable.Empty<NewsArticle>();
                }
                articles = await _newsArticleRepository.GetBySourceAsync(source.Name, 1, count, cancellationToken);
            }
            else
            {
                articles = await _newsArticleRepository.GetLatestAsync(1, count, cancellationToken);
            }
            
            return articles;
        }
    }
} 