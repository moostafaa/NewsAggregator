using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.Management.Models
{
    public class CategoryCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public NewsProviderType ProviderType { get; set; }
        public string ProviderSpecificKey { get; set; }
    }
} 