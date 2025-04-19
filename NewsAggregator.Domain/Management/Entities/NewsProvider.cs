using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.Management.Entities
{
    public class NewsProvider : AggregateRoot
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public NewsProviderType ProviderType { get; private set; }
        public string ApiKey { get; private set; }
        public string BaseUrl { get; private set; }
        public bool IsActive { get; private set; }
        public Dictionary<string, string> AdditionalSettings { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // For EF Core
        private NewsProvider() { }

        private NewsProvider(
            Guid id,
            string name,
            string description,
            NewsProviderType providerType,
            string apiKey,
            string baseUrl,
            bool isActive,
            Dictionary<string, string> additionalSettings = null) : base(id)
        {
            Name = name;
            Description = description;
            ProviderType = providerType;
            ApiKey = apiKey;
            BaseUrl = baseUrl;
            IsActive = isActive;
            AdditionalSettings = additionalSettings ?? new Dictionary<string, string>();
            CreatedAt = DateTime.UtcNow;
        }

        public static NewsProvider Create(
            string name,
            string description,
            NewsProviderType providerType,
            string apiKey,
            string baseUrl,
            bool isActive = true,
            Dictionary<string, string> additionalSettings = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Provider name cannot be empty");

            return new NewsProvider(
                Guid.NewGuid(),
                name,
                description,
                providerType,
                apiKey,
                baseUrl,
                isActive,
                additionalSettings);
        }

        public void UpdateDetails(
            string name,
            string description,
            string apiKey,
            string baseUrl,
            Dictionary<string, string> additionalSettings = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name;

            Description = description;
            ApiKey = apiKey;
            BaseUrl = baseUrl;

            if (additionalSettings != null)
                AdditionalSettings = additionalSettings;

            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
} 