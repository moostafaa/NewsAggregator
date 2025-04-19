using System.Collections.Generic;

namespace NewsAggregator.Domain.Management.Models
{
    public class ProviderConfigUpdateModel
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public Dictionary<string, string> AdditionalSettings { get; set; }
    }
} 