using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.Management.ValueObjects;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.Management.Entities
{
    public class CloudConfig : AggregateRoot
    {
        public CloudProvider Provider { get; private set; }
        public string Region { get; private set; }
        public Dictionary<string, string> Settings { get; private set; }
        public CloudCredentials Credentials { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // For EF Core
        private CloudConfig() { }

        private CloudConfig(
            Guid id,
            CloudProvider provider,
            string region,
            CloudCredentials credentials,
            Dictionary<string, string> settings = null) : base(id)
        {
            Provider = provider;
            Region = region;
            Credentials = credentials;
            Settings = settings ?? new Dictionary<string, string>();
            CreatedAt = DateTime.UtcNow;
        }

        public static CloudConfig Create(
            CloudProvider provider,
            string region,
            CloudCredentials credentials,
            Dictionary<string, string> settings = null)
        {
            if (string.IsNullOrWhiteSpace(region))
                throw new DomainException("Region cannot be empty");

            if (credentials == null)
                throw new DomainException("Credentials cannot be null");

            return new CloudConfig(
                Guid.NewGuid(),
                provider,
                region,
                credentials,
                settings);
        }

        public void UpdateConfig(
            CloudProvider provider,
            string region,
            CloudCredentials credentials,
            Dictionary<string, string> settings = null)
        {
            Provider = provider;
            
            if (!string.IsNullOrWhiteSpace(region))
                Region = region;
            
            if (credentials != null)
                Credentials = credentials;
            
            if (settings != null)
                Settings = settings;
            
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum CloudProvider
    {
        AWS,
        Azure,
        GCP
    }
} 