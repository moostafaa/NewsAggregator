using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateCategoryAddedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string Category { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateCategoryAddedEvent(Guid aggregateId, string category)
        {
            AggregateId = aggregateId;
            Category = category;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 