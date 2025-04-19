using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Events;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Entities
{
    public class RssSource : AggregateRoot
    {
        public string Name { get; private set; }
        public string Url { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime LastFetchedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public NewsProviderType ProviderType { get; private set; }
        public string DefaultCategory { get; private set; }

        private RssSource() { } // For EF Core

        private RssSource(Guid id, string name, string url, string description, 
            NewsProviderType providerType, string defaultCategory = "general") : base(id)
        {
            Name = name;
            Url = url;
            Description = description ?? string.Empty;
            IsActive = true;
            LastFetchedAt = DateTime.MinValue;
            CreatedAt = DateTime.UtcNow;
            ProviderType = providerType;
            DefaultCategory = defaultCategory;
        }

        public static RssSource Create(string name, string url, string description, 
            NewsProviderType providerType, string defaultCategory = "general")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException("Invalid URL format", nameof(url));

            var source = new RssSource(
                Guid.NewGuid(),
                name.Trim(),
                url.Trim(),
                description,
                providerType,
                defaultCategory
            );

            return source;
        }

        public void Update(string name, string url, string description, 
            NewsProviderType providerType, string defaultCategory = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException("Invalid URL format", nameof(url));

            Name = name.Trim();
            Url = url.Trim();
            Description = description?.Trim() ?? Description;
            ProviderType = providerType;
            DefaultCategory = defaultCategory ?? DefaultCategory;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateLastFetchedTime()
        {
            LastFetchedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
} 