using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Data;
using NewsAggregator.Crawler.Models;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Protos;

namespace NewsAggregator.Crawler.Services
{
    /// <summary>
    /// Service that receives categories via gRPC from the main application
    /// </summary>
    public class GrpcCategoryService : ICategoryService
    {
        private readonly CategoryService.CategoryServiceClient _client;
        private readonly NewsCrawlerDbContext _dbContext;
        private readonly ILogger<GrpcCategoryService> _logger;
        private readonly DistributedCrawlerOptions _options;

        public GrpcCategoryService(
            CategoryService.CategoryServiceClient client,
            NewsCrawlerDbContext dbContext,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<GrpcCategoryService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Refreshes the local database with categories from the main application
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of categories updated</returns>
        public async Task<int> RefreshCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Refreshing categories from the main application");

                // Get all categories from the gRPC service
                var request = new CategoryRequest { IncludeInactive = true };
                var response = await _client.GetCategoriesAsync(request, cancellationToken: cancellationToken);

                if (response?.Categories?.Count == 0)
                {
                    _logger.LogWarning("No categories received from the main application");
                    return 0;
                }

                // Get existing categories from the database
                var existingCategories = await _dbContext.Categories.ToDictionaryAsync(
                    c => c.Id,
                    c => c,
                    cancellationToken);

                int added = 0;
                int updated = 0;

                // Update existing categories or add new ones
                foreach (var categoryItem in response.Categories)
                {
                    if (existingCategories.TryGetValue(categoryItem.Id, out var existingCategory))
                    {
                        // Update existing category
                        existingCategory.Name = categoryItem.Name;
                        existingCategory.Slug = categoryItem.Slug;
                        existingCategory.Description = categoryItem.Description;
                        existingCategory.ProviderType = categoryItem.ProviderType;
                        existingCategory.IsActive = categoryItem.IsActive;
                        existingCategory.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        // Add new category
                        var newCategory = Category.FromGrpc(categoryItem);
                        _dbContext.Categories.Add(newCategory);
                        added++;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Refreshed categories: {Added} added, {Updated} updated", added, updated);

                return added + updated;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error refreshing categories via gRPC: {Status}", ex.Status);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing categories");
                throw;
            }
        }

        /// <summary>
        /// Classifies an article into a category
        /// </summary>
        /// <param name="title">Article title</param>
        /// <param name="content">Article content</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="sourceCategory">Original source category</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The category for the article</returns>
        public async Task<Category> ClassifyArticleAsync(
            string title,
            string content,
            string sourceName = null,
            string sourceCategory = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new ClassificationRequest
                {
                    Title = title,
                    Content = content,
                    SourceName = sourceName ?? string.Empty,
                    SourceCategory = sourceCategory ?? string.Empty
                };

                var response = await _client.ClassifyArticleAsync(request, cancellationToken: cancellationToken);

                // Find the category in the database
                var category = await _dbContext.Categories
                    .FirstOrDefaultAsync(c => c.Id == response.CategoryId, cancellationToken);

                // If category not found, try to find by name
                if (category == null)
                {
                    category = await _dbContext.Categories
                        .FirstOrDefaultAsync(c => c.Name == response.CategoryName, cancellationToken);

                    // If still not found, get the default category or create one
                    if (category == null)
                    {
                        category = await _dbContext.Categories
                            .FirstOrDefaultAsync(c => c.Slug == "uncategorized", cancellationToken);

                        if (category == null)
                        {
                            category = new Category
                            {
                                Name = "Uncategorized",
                                Slug = "uncategorized",
                                Description = "Default category for unclassified articles",
                                IsActive = true
                            };
                            _dbContext.Categories.Add(category);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                return category;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error classifying article via gRPC: {Status}", ex.Status);
                
                // Fallback to a default category
                var defaultCategory = await GetOrCreateDefaultCategoryAsync(cancellationToken);
                return defaultCategory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying article");
                
                // Fallback to a default category
                var defaultCategory = await GetOrCreateDefaultCategoryAsync(cancellationToken);
                return defaultCategory;
            }
        }

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive categories</param>
        /// <param name="providerType">Optional provider type filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of categories</returns>
        public async Task<IEnumerable<Category>> GetCategoriesAsync(
            bool includeInactive = false,
            string providerType = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbContext.Categories.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }

                if (!string.IsNullOrEmpty(providerType))
                {
                    query = query.Where(c => c.ProviderType == providerType);
                }

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return Enumerable.Empty<Category>();
            }
        }

        /// <summary>
        /// Gets or creates a default category
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The default category</returns>
        private async Task<Category> GetOrCreateDefaultCategoryAsync(CancellationToken cancellationToken)
        {
            var defaultCategory = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Slug == "uncategorized", cancellationToken);

            if (defaultCategory == null)
            {
                defaultCategory = new Category
                {
                    Name = "Uncategorized",
                    Slug = "uncategorized",
                    Description = "Default category for unclassified articles",
                    IsActive = true
                };
                _dbContext.Categories.Add(defaultCategory);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return defaultCategory;
        }
    }
} 