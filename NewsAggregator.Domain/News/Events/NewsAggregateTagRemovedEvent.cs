using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateTagRemovedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string Tag { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateTagRemovedEvent(Guid aggregateId, string tag)
        {
            AggregateId = aggregateId;
            Tag = tag;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 