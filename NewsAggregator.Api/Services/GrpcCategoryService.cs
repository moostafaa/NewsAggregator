using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsAggregator.Api.Protos;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;

namespace NewsAggregator.Api.Services
{
    /// <summary>
    /// gRPC service implementation for providing news categories to crawler microservices
    /// </summary>
    public class GrpcCategoryService : CategoryService.CategoryServiceBase
    {
        private readonly INewsCategoryRepository _categoryRepository;
        private readonly ICategoryClassificationService _classificationService;
        private readonly ILogger<GrpcCategoryService> _logger;

        public GrpcCategoryService(
            INewsCategoryRepository categoryRepository,
            ICategoryClassificationService classificationService,
            ILogger<GrpcCategoryService> logger)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all categories from the database
        /// </summary>
        public override async Task<CategoryResponse> GetCategories(CategoryRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Getting categories. IncludeInactive: {IncludeInactive}, ProviderType: {ProviderType}",
                    request.IncludeInactive, request.ProviderType);

                // Get categories based on request parameters
                IEnumerable<NewsCategory> categories;
                
                if (!string.IsNullOrEmpty(request.ProviderType))
                {
                    if (Enum.TryParse<Domain.News.Enums.NewsProviderType>(
                        request.ProviderType, true, out var providerType))
                    {
                        categories = await _categoryRepository.GetByProviderTypeAsync(providerType);
                    }
                    else
                    {
                        categories = await _categoryRepository.GetActiveAsync();
                    }
                }
                else
                {
                    categories = request.IncludeInactive 
                        ? await _categoryRepository.GetAllAsync() 
                        : await _categoryRepository.GetActiveAsync();
                }

                // Create response with category items
                var response = new CategoryResponse();
                
                foreach (var category in categories)
                {
                    response.Categories.Add(new CategoryItem
                    {
                        Id = category.Id.ToString(),
                        Name = category.Name,
                        Slug = category.Slug,
                        Description = category.Description ?? string.Empty,
                        ProviderType = category.ProviderType.ToString(),
                        IsActive = category.IsActive
                    });
                }

                _logger.LogInformation("Returning {Count} categories", response.Categories.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw new RpcException(new Status(StatusCode.Internal, "Error getting categories"));
            }
        }

        /// <summary>
        /// Streams categories as they are updated in real time
        /// </summary>
        public override async Task StreamCategories(CategoryRequest request, IServerStreamWriter<CategoryItem> responseStream, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Streaming categories. IncludeInactive: {IncludeInactive}, ProviderType: {ProviderType}",
                    request.IncludeInactive, request.ProviderType);

                // Get categories based on request parameters (similar to GetCategories)
                IEnumerable<NewsCategory> categories;
                
                if (!string.IsNullOrEmpty(request.ProviderType))
                {
                    if (Enum.TryParse<Domain.News.Enums.NewsProviderType>(
                        request.ProviderType, true, out var providerType))
                    {
                        categories = await _categoryRepository.GetByProviderTypeAsync(providerType);
                    }
                    else
                    {
                        categories = await _categoryRepository.GetActiveAsync();
                    }
                }
                else
                {
                    categories = request.IncludeInactive 
                        ? await _categoryRepository.GetAllAsync() 
                        : await _categoryRepository.GetActiveAsync();
                }

                // Stream each category as a separate message
                foreach (var category in categories)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;

                    await responseStream.WriteAsync(new CategoryItem
                    {
                        Id = category.Id.ToString(),
                        Name = category.Name,
                        Slug = category.Slug,
                        Description = category.Description ?? string.Empty,
                        ProviderType = category.ProviderType.ToString(),
                        IsActive = category.IsActive
                    });
                }

                _logger.LogInformation("Streamed {Count} categories", categories.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming categories");
                throw new RpcException(new Status(StatusCode.Internal, "Error streaming categories"));
            }
        }

        /// <summary>
        /// Classifies an article into a category
        /// </summary>
        public override async Task<ClassificationResponse> ClassifyArticle(ClassificationRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Classifying article: {Title}", request.Title);

                // Use the domain service to classify the article
                string category = await _classificationService.ClassifyArticleAsync(
                    request.Title,
                    request.Content,
                    request.SourceName,
                    request.SourceCategory);

                // Find the category in the repository by name
                var newsCategory = await _categoryRepository.GetByNameAsync(category);
                
                // If not found, try to use a default category
                if (newsCategory == null)
                {
                    newsCategory = await _categoryRepository.GetBySlugAsync("uncategorized");
                    
                    // If still not found, get the first active category
                    if (newsCategory == null)
                    {
                        var activeCategories = await _categoryRepository.GetActiveAsync();
                        newsCategory = activeCategories.FirstOrDefault();
                    }
                }

                // Create the response
                var response = new ClassificationResponse
                {
                    CategoryId = newsCategory?.Id.ToString() ?? string.Empty,
                    CategoryName = newsCategory?.Name ?? "Uncategorized",
                    ConfidenceScore = 1.0f // No actual confidence score in this implementation
                };

                _logger.LogInformation("Classified article into category: {Category}", response.CategoryName);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying article");
                throw new RpcException(new Status(StatusCode.Internal, "Error classifying article"));
            }
        }
    }
} 