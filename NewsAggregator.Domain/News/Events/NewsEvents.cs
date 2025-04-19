using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsAggregateCreatedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public DateTime OccurredOn { get; }

        public NewsAggregateCreatedEvent(Guid aggregateId)
        {
            AggregateId = aggregateId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class NewsArticleAddedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public Guid ArticleId { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleAddedEvent(Guid aggregateId, Guid articleId)
        {
            AggregateId = aggregateId;
            ArticleId = articleId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class NewsSourceAddedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string SourceName { get; }
        public DateTime OccurredOn { get; }

        public NewsSourceAddedEvent(Guid aggregateId, string sourceName)
        {
            AggregateId = aggregateId;
            SourceName = sourceName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class NewsSourceRemovedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string SourceName { get; }
        public DateTime OccurredOn { get; }

        public NewsSourceRemovedEvent(Guid aggregateId, string sourceName)
        {
            AggregateId = aggregateId;
            SourceName = sourceName;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 