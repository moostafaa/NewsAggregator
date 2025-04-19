using System;
using System.Collections.Generic;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.Management.Entities;

namespace NewsAggregator.Domain.Management.Models
{
    public class ProviderConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NewsProviderType ProviderType { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Dictionary<string, string> AdditionalSettings { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static ProviderConfig FromNewsProvider(NewsProvider provider)
        {
            return new ProviderConfig
            {
                Id = provider.Id,
                Name = provider.Name,
                Description = provider.Description,
                ProviderType = provider.ProviderType,
                ApiKey = provider.ApiKey,
                BaseUrl = provider.BaseUrl,
                IsActive = provider.IsActive,
                AdditionalSettings = provider.AdditionalSettings,
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt
            };
        }
    }
} 