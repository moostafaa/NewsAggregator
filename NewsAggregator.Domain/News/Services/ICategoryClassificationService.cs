using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregator.Domain.News.Services
{
    public interface ICategoryClassificationService
    {
        /// <summary>
        /// Classifies a news article title and content into the most appropriate category
        /// </summary>
        /// <param name="title">The article title</param>
        /// <param name="content">The article content or summary</param>
        /// <param name="sourceName">The name of the source</param>
        /// <param name="sourceCategory">The original category from the source (if available)</param>
        /// <returns>The standardized category name in the system</returns>
        Task<string> ClassifyArticleAsync(string title, string content, string sourceName, string sourceCategory = null);
        
        /// <summary>
        /// Maps a source-specific category to a standard system category
        /// </summary>
        /// <param name="sourceName">The name of the source</param>
        /// <param name="sourceCategory">The category as defined by the source</param>
        /// <returns>The standardized category name in the system</returns>
        Task<string> MapSourceCategoryAsync(string sourceName, string sourceCategory);
        
        /// <summary>
        /// Gets all valid system categories
        /// </summary>
        /// <returns>List of valid system categories</returns>
        Task<IEnumerable<string>> GetValidCategoriesAsync();
    }
} 