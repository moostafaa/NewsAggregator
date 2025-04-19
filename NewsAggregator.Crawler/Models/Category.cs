using System;
using System.Collections.Generic;

namespace NewsAggregator.Crawler.Models
{
    /// <summary>
    /// Represents a news category in the crawler microservice
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Unique identifier for the category
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the category
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// URL-friendly version of the name (for routing)
        /// </summary>
        public string Slug { get; set; }
        
        /// <summary>
        /// Optional description of the category
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The provider type this category is associated with
        /// </summary>
        public string ProviderType { get; set; }
        
        /// <summary>
        /// Whether this category is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// When this category was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this category was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// List of crawled sources that are associated with this category
        /// </summary>
        public virtual ICollection<CrawledSource> Sources { get; set; } = new List<CrawledSource>();
        
        /// <summary>
        /// Creates a new category instance from a gRPC category item
        /// </summary>
        /// <param name="categoryItem">The gRPC category item</param>
        /// <returns>A new Category instance</returns>
        public static Category FromGrpc(Protos.CategoryItem categoryItem)
        {
            return new Category
            {
                Id = categoryItem.Id,
                Name = categoryItem.Name,
                Slug = categoryItem.Slug,
                Description = categoryItem.Description,
                ProviderType = categoryItem.ProviderType,
                IsActive = categoryItem.IsActive
            };
        }
        
        /// <summary>
        /// Converts this category to a gRPC category item
        /// </summary>
        /// <returns>A gRPC category item</returns>
        public Protos.CategoryItem ToGrpc()
        {
            return new Protos.CategoryItem
            {
                Id = Id,
                Name = Name,
                Slug = Slug,
                Description = Description ?? string.Empty,
                ProviderType = ProviderType ?? string.Empty,
                IsActive = IsActive
            };
        }
    }
} 