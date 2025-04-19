using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Management.Entities;
using NewsAggregator.Domain.Management.ValueObjects;

namespace NewsAggregator.Domain.Management.Models
{
    public class CloudConfiguration
    {
        public Guid Id { get; set; }
        public CloudProvider Provider { get; set; }
        public string Region { get; set; } = string.Empty;
        public CloudCredentials Credentials { get; set; } = null!;
        public Dictionary<string, string> Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static CloudConfiguration FromCloudConfig(CloudConfig config)
        {
            return new CloudConfiguration
            {
                Id = config.Id,
                Provider = config.Provider,
                Region = config.Region,
                Credentials = config.Credentials,
                Settings = config.Settings,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }
    }
} 