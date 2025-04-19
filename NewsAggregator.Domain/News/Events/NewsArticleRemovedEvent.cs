using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsArticleRemovedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public Guid ArticleId { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleRemovedEvent(Guid aggregateId, Guid articleId)
        {
            AggregateId = aggregateId;
            ArticleId = articleId;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 