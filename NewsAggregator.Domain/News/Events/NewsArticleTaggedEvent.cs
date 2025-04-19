using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsArticleTaggedEvent : IDomainEvent
    {
        public NewsArticle Article { get; }
        public string Tag { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleTaggedEvent(NewsArticle article, string tag)
        {
            Article = article;
            Tag = tag;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 