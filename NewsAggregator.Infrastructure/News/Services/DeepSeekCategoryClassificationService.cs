using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Infrastructure.Options;

namespace NewsAggregator.Infrastructure.News.Services
{
    public class DeepSeekCategoryClassificationService : ICategoryClassificationService
    {
        private readonly HttpClient _httpClient;
        private readonly INewsCategoryRepository _categoryRepository;
        private readonly ILogger<DeepSeekCategoryClassificationService> _logger;
        private readonly DeepSeekOptions _options;

        public DeepSeekCategoryClassificationService(
            HttpClient httpClient,
            INewsCategoryRepository categoryRepository,
            IOptions<DeepSeekOptions> options,
            ILogger<DeepSeekCategoryClassificationService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> ClassifyArticleAsync(string title, string content, string sourceName, string sourceCategory = null)
        {
            // First try to map the source category if available
            if (!string.IsNullOrWhiteSpace(sourceCategory) && 
                !string.IsNullOrWhiteSpace(sourceName))
            {
                var mappedCategory = await MapSourceCategoryAsync(sourceName, sourceCategory);
                if (!string.IsNullOrWhiteSpace(mappedCategory) && mappedCategory != "uncategorized")
                {
                    return mappedCategory;
                }
            }
            
            try
            {
                // Get all active categories from the database
                var validCategories = await GetValidCategoriesAsync();
                var categoryList = string.Join(", ", validCategories);
                
                if (string.IsNullOrWhiteSpace(categoryList))
                {
                    _logger.LogWarning("No valid categories found in the database. Using uncategorized as fallback.");
                    return "uncategorized";
                }

                // Prepare the prompt for DeepSeek
                var prompt = $@"
Classify the following news article into exactly one of these categories: 
{categoryList}.

Title: {title}
Summary: {content}

Return only the category name without any explanation or additional text.";

                // Call DeepSeek API
                var requestData = new
                {
                    model = _options.ModelName,
                    prompt = prompt,
                    max_tokens = 30,
                    temperature = 0.2
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiEndpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json")
                };
                
                request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<DeepSeekResponse>(jsonResponse);

                // Extract the category from the response
                var predictedCategory = responseObject?.Choices?[0]?.Text?.Trim().ToLower() ?? "uncategorized";
                
                // Validate if the returned category is in our valid list
                if (validCategories.Contains(predictedCategory))
                {
                    return predictedCategory;
                }
                
                // If not a valid category, try to find the closest match
                foreach (var validCategory in validCategories)
                {
                    if (predictedCategory.Contains(validCategory) || validCategory.Contains(predictedCategory))
                    {
                        return validCategory;
                    }
                }
                
                return "uncategorized";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying article with DeepSeek: {Title}", title);
                
                // Fallback to source category if available, or "uncategorized"
                return !string.IsNullOrWhiteSpace(sourceCategory) 
                    ? await MapSourceCategoryAsync(sourceName, sourceCategory) 
                    : "uncategorized";
            }
        }

        public async Task<string> MapSourceCategoryAsync(string sourceName, string sourceCategory)
        {
            if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(sourceCategory))
            {
                return "uncategorized";
            }
            
            try
            {
                // Get all active categories from the database
                var validCategories = await GetValidCategoriesAsync();
                
                // Direct matching if the source category matches a system category
                var directMatch = validCategories.FirstOrDefault(c => 
                    c.Equals(sourceCategory, StringComparison.OrdinalIgnoreCase));
                    
                if (!string.IsNullOrEmpty(directMatch))
                {
                    return directMatch;
                }
                
                // Try closest match based on string similarity
                foreach (var category in validCategories)
                {
                    if (sourceCategory.Contains(category, StringComparison.OrdinalIgnoreCase) || 
                        category.Contains(sourceCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        return category;
                    }
                }
                
                // If no mapping found, use DeepSeek to classify
                var prompt = $@"
Map the following news article category '{sourceCategory}' from source '{sourceName}' 
to one of these categories: {string.Join(", ", validCategories)}.

Return only the category name without any explanation or additional text.";

                var requestData = new
                {
                    model = _options.ModelName,
                    prompt = prompt,
                    max_tokens = 30,
                    temperature = 0.2
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiEndpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json")
                };
                
                request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<DeepSeekResponse>(jsonResponse);
                
                var mappedCategory = responseObject?.Choices?[0]?.Text?.Trim().ToLower() ?? "uncategorized";
                
                if (validCategories.Contains(mappedCategory))
                {
                    return mappedCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping category using DeepSeek: {SourceName} - {Category}", 
                    sourceName, sourceCategory);
            }
            
            // If all else fails, return "uncategorized"
            return "uncategorized";
        }

        public async Task<IEnumerable<string>> GetValidCategoriesAsync()
        {
            try
            {
                // Get active categories from database
                var categories = await _categoryRepository.GetActiveAsync();
                var categoryNames = categories.Select(c => c.Name.ToLowerInvariant()).ToList();
                
                // Ensure we always have "uncategorized" as a valid category
                if (!categoryNames.Contains("uncategorized"))
                {
                    categoryNames.Add("uncategorized");
                }
                
                return categoryNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting valid categories from repository");
                return new List<string> { "uncategorized" };
            }
        }

        private class DeepSeekResponse
        {
            public Choice[] Choices { get; set; }
            
            public class Choice
            {
                public string Text { get; set; }
            }
        }
    }
} 