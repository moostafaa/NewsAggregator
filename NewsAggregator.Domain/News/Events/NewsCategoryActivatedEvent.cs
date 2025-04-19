using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsCategoryActivatedEvent : IDomainEvent
    {
        public NewsCategory Category { get; }
        public DateTime OccurredOn { get; }

        public NewsCategoryActivatedEvent(NewsCategory category)
        {
            Category = category;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 