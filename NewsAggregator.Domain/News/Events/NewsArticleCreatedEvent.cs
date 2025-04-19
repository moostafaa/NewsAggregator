using System;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Events
{
    public class NewsArticleCreatedEvent : IDomainEvent
    {
        public Guid ArticleId { get; }
        public string Title { get; }
        public string Source { get; }
        public string Category { get; }
        public DateTime OccurredOn { get; }

        public NewsArticleCreatedEvent(NewsArticle article)
        {
            ArticleId = article.Id;
            Title = article.Body;
            Source = article.Source.Name;
            Category = article.Category;
            OccurredOn = DateTime.UtcNow;
        }
    }
} 