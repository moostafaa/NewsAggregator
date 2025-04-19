using System;
using System.Collections.Generic;
using System.Linq;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Events;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.News.Entities
{
    public class NewsArticle : AggregateRoot
    {
        public string Title { get; private set; }
        public string Summary { get; private set; }
        public string Body { get; private set; }
        public NewsSource Source { get; private set; }
        public DateTime PublishedDate { get; private set; }
        public string Category { get; private set; }
        public Uri Url { get; private set; }
        public bool IsSaved { get; private set; }
        public bool IsRead { get; private set; }
        public int LikeCount { get; private set; }
        public List<string> Tags { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private NewsArticle(
            Guid id,
            string title,
            string summary,
            string body,
            NewsSource source,
            DateTime publishedDate,
            string category,
            Uri url,
            List<string> tags) : base(id)
        {
            Title = title;
            Summary = summary;
            Body = body;
            Source = source;
            PublishedDate = publishedDate;
            Category = category;
            Url = url;
            Tags = tags ?? new List<string>();
            IsSaved = false;
            IsRead = false;
            LikeCount = 0;
            CreatedAt = DateTime.UtcNow;
        }

        public static NewsArticle Create(
            string title,
            string summary,
            string body,
            NewsSource source,
            DateTime publishedDate,
            string category,
            string url,
            IEnumerable<string> tags = null)
        {
            ValidateArticle(title, body, source, url);

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new DomainException("Invalid URL format");

            var normalizedTags = NormalizeTags(tags);
            
            var article = new NewsArticle(
                Guid.NewGuid(),
                title.Trim(),
                summary?.Trim() ?? string.Empty,
                body.Trim(),
                source,
                publishedDate,
                NormalizeCategory(category),
                uri,
                normalizedTags);
            
            article.AddDomainEvent(new NewsArticleCreatedEvent(article));
            
            return article;
        }

        public void UpdateContent(string title, string summary, string body)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title cannot be null or empty");

            if (string.IsNullOrWhiteSpace(body))
                throw new DomainException("Body cannot be null or empty");

            Title = title.Trim();
            Summary = summary?.Trim() ?? string.Empty;
            Body = body.Trim();
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsArticleUpdatedEvent(this));
        }

        public void UpdateCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new DomainException("Category cannot be empty");

            Category = NormalizeCategory(category);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsArticleUpdatedEvent(this));
        }
        
        public void UpdateSource(NewsSource source)
        {
            if (source == null)
                throw new DomainException("Source cannot be null");

            Source = source;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsArticleUpdatedEvent(this));
        }
        
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Tag cannot be empty");
                
            string normalizedTag = tag.Trim().ToLower();
            
            if (!Tags.Contains(normalizedTag))
            {
                Tags.Add(normalizedTag);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleTaggedEvent(this, normalizedTag));
            }
        }
        
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Tag cannot be empty");
                
            string normalizedTag = tag.Trim().ToLower();
            
            if (Tags.Contains(normalizedTag))
            {
                Tags.Remove(normalizedTag);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleUntaggedEvent(this, normalizedTag));
            }
        }
        
        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleReadEvent(this));
            }
        }
        
        public void MarkAsSaved()
        {
            if (!IsSaved)
            {
                IsSaved = true;
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleSavedEvent(this));
            }
        }
        
        public void RemoveFromSaved()
        {
            if (IsSaved)
            {
                IsSaved = false;
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleUnsavedEvent(this));
            }
        }
        
        public void Like()
        {
            LikeCount++;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsArticleLikedEvent(this));
        }
        
        private static void ValidateArticle(string title, string body, NewsSource source, string url)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title cannot be null or empty");
                
            if (string.IsNullOrWhiteSpace(body))
                throw new DomainException("Body cannot be null or empty");

            if (source == null)
                throw new DomainException("Source cannot be null");
                
            if (string.IsNullOrWhiteSpace(url))
                throw new DomainException("URL cannot be empty");
        }
        
        private static string NormalizeCategory(string category)
        {
            return string.IsNullOrWhiteSpace(category) 
                ? "uncategorized" 
                : category.Trim().ToLower();
        }
        
        private static List<string> NormalizeTags(IEnumerable<string> tags)
        {
            var result = new List<string>();
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        string normalizedTag = tag.Trim().ToLower();
                        if (!result.Contains(normalizedTag))
                        {
                            result.Add(normalizedTag);
                        }
                    }
                }
            }
            
            return result;
        }
    }
} 