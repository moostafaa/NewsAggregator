using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateDeactivatedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateDeactivatedEvent(Guid aggregateId)
        {
            AggregateId = aggregateId;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 