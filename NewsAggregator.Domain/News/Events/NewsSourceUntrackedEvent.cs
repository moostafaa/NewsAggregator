using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsSourceUntrackedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string SourceName { get; }
        public DateTime OccurredOn { get; }

        public NewsSourceUntrackedEvent(Guid aggregateId, string sourceName)
        {
            AggregateId = aggregateId;
            SourceName = sourceName;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 