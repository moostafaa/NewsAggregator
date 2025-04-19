using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateActivatedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateActivatedEvent(Guid aggregateId)
        {
            AggregateId = aggregateId;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 