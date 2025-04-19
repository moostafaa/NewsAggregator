using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsArticleSavedEvent : IDomainEvent
    {
        public NewsArticle Article { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleSavedEvent(NewsArticle article)
        {
            Article = article;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 