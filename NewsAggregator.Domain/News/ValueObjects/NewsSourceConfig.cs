using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.ValueObjects
{
    public class NewsSourceConfig : ValueObject
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public NewsProviderType ProviderType { get; set; }
        public IReadOnlyList<string> SupportedCategories { get; set; }
        public bool IsEnabled { get; set; }
        public Dictionary<string, string> AdditionalConfig { get; set; }

        public NewsSourceConfig()
        {
            // Default constructor for simplifying testing
            SupportedCategories = new List<string>().AsReadOnly();
            AdditionalConfig = new Dictionary<string, string>();
            IsEnabled = true;
        }

        private NewsSourceConfig(
            string name,
            string baseUrl,
            string apiKey,
            NewsProviderType providerType,
            IReadOnlyList<string> supportedCategories,
            bool isEnabled,
            Dictionary<string, string> additionalConfig)
        {
            Name = name;
            BaseUrl = baseUrl;
            ApiKey = apiKey;
            ProviderType = providerType;
            SupportedCategories = supportedCategories;
            IsEnabled = isEnabled;
            AdditionalConfig = additionalConfig ?? new Dictionary<string, string>();
        }

        public static NewsSourceConfig Create(
            string name,
            string baseUrl,
            string apiKey,
            NewsProviderType providerType,
            IEnumerable<string> supportedCategories,
            bool isEnabled = true,
            Dictionary<string, string> additionalConfig = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("News source name cannot be empty");

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new DomainException("Base URL cannot be empty");

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                throw new DomainException("Invalid base URL format");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new DomainException("API key cannot be empty");

            var categories = new List<string>();
            if (supportedCategories != null)
            {
                foreach (var category in supportedCategories)
                {
                    if (!string.IsNullOrWhiteSpace(category))
                        categories.Add(category.Trim().ToLower());
                }
            }

            return new NewsSourceConfig(
                name.Trim(),
                baseUrl.Trim(),
                apiKey,
                providerType,
                categories.AsReadOnly(),
                isEnabled,
                additionalConfig);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return BaseUrl;
            yield return ApiKey;
            yield return ProviderType;
            if (SupportedCategories != null)
            {
                foreach (var category in SupportedCategories)
                {
                    yield return category;
                }
            }
            yield return IsEnabled;
            if (AdditionalConfig != null)
            {
                foreach (var config in AdditionalConfig)
                {
                    yield return config.Key;
                    yield return config.Value;
                }
            }
        }
    }
} 