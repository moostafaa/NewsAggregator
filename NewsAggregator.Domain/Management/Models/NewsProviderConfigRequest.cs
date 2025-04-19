using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.Management.Models
{
    public class NewsProviderConfigRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public NewsProviderType ProviderType { get; set; }
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public bool IsActive { get; set; }
    }
} 