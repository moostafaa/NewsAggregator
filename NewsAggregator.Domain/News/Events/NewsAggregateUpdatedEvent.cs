using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateUpdatedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateUpdatedEvent(Guid aggregateId)
        {
            AggregateId = aggregateId;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 