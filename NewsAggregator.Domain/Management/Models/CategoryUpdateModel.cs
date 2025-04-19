namespace NewsAggregator.Domain.Management.Models
{
    public class CategoryUpdateModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProviderSpecificKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
} 