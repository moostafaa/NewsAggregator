using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.ValueObjects
{
    public class NewsSource : ValueObject
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Uri Url { get; private set; }
        public IReadOnlyList<string> Categories { get; private set; }
        public string ProviderType { get; private set; }

        private NewsSource(string name, Uri url, IReadOnlyList<string> categories)
        {
            Id = Guid.NewGuid();
            Name = name;
            Url = url;
            Categories = categories;
        }

        private NewsSource(Guid id, string name, string providerType)
        {
            Id = id;
            Name = name;
            ProviderType = providerType;
            Categories = new List<string>().AsReadOnly();
        }

        public static NewsSource Create(string name, string url, IEnumerable<string> categories)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Source name cannot be empty");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new DomainException("Invalid URL format");

            var categoryList = new List<string>();
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    if (!string.IsNullOrWhiteSpace(category))
                        categoryList.Add(category.Trim().ToLower());
                }
            }

            return new NewsSource(name.Trim(), uri, categoryList.AsReadOnly());
        }
        
        public static NewsSource Create(Guid id, string name, string providerType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Source name cannot be empty");
                
            if (string.IsNullOrWhiteSpace(providerType))
                throw new DomainException("Provider type cannot be empty");
                
            return new NewsSource(id, name, providerType);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
            yield return Name;
            if (Url != null) yield return Url;
            if (ProviderType != null) yield return ProviderType;
            if (Categories != null)
            {
                foreach (var category in Categories)
                {
                    yield return category;
                }
            }
        }
    }
} 