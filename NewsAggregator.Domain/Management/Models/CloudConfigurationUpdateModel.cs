using System.Collections.Generic;
using NewsAggregator.Domain.Management.Entities;
using NewsAggregator.Domain.Management.ValueObjects;

namespace NewsAggregator.Domain.Management.Models
{
    public class CloudConfigurationUpdateModel
    {
        public CloudProvider Provider { get; set; }
        public CloudCredentials Credentials { get; set; }
        public string Region { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
} 