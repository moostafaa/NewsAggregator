using System;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.Management.Models
{
    public class NewsProviderConfigResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public NewsProviderType ProviderType { get; set; }
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 