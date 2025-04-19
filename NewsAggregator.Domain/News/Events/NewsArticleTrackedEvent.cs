using System;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsArticleTrackedEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public Guid ArticleId { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleTrackedEvent(Guid aggregateId, Guid articleId)
        {
            AggregateId = aggregateId;
            ArticleId = articleId;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 