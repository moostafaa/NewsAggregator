using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsCategoryCreatedEvent : IDomainEvent
    {
        public Guid CategoryId { get; }
        public string Name { get; }
        public string Slug { get; }
        public NewsProviderType ProviderType { get; }
        public DateTime OccurredOn { get; }

        public NewsCategoryCreatedEvent(NewsCategory category)
        {
            CategoryId = category.Id;
            Name = category.Name;
            Slug = category.Slug;
            ProviderType = category.ProviderType;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 