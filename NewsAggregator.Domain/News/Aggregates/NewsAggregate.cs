using System;
using System.Collections.Generic;
using System.Linq;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Events;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.News.Aggregates
{
    public class NewsAggregate : AggregateRoot
    {
        // We store references/identifiers rather than actual entities
        private readonly List<Guid> _articleIds;
        private readonly List<string> _sourceNames;
        private readonly List<string> _tags;
        private readonly List<string> _categories;
        
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private NewsAggregate() : base()
        {
            _articleIds = new List<Guid>();
            _sourceNames = new List<string>();
            _tags = new List<string>();
            _categories = new List<string>();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
        
        private NewsAggregate(Guid id, string name, string description) : base(id)
        {
            Name = name;
            Description = description;
            _articleIds = new List<Guid>();
            _sourceNames = new List<string>();
            _tags = new List<string>();
            _categories = new List<string>();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public static NewsAggregate Create(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Aggregate name cannot be empty");
                
            var aggregate = new NewsAggregate(Guid.NewGuid(), name.Trim(), description?.Trim() ?? string.Empty);
            aggregate.AddDomainEvent(new NewsAggregateCreatedEvent(aggregate.Id));
            return aggregate;
        }
        
        public void Update(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Aggregate name cannot be empty");
                
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsAggregateUpdatedEvent(Id));
        }
        
        public void Deactivate()
        {
            if (!IsActive)
                return;
                
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsAggregateDeactivatedEvent(Id));
        }
        
        public void Activate()
        {
            if (IsActive)
                return;
                
            IsActive = true; 
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsAggregateActivatedEvent(Id));
        }

        public void TrackArticle(Guid articleId)
        {
            if (articleId == Guid.Empty)
                throw new DomainException("Article ID cannot be empty");

            if (!_articleIds.Contains(articleId))
            {
                _articleIds.Add(articleId);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleTrackedEvent(Id, articleId));
            }
        }

        public void UntrackArticle(Guid articleId)
        {
            if (_articleIds.Contains(articleId))
            {
                _articleIds.Remove(articleId);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsArticleUntrackedEvent(Id, articleId));
            }
        }

        public void TrackSource(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new DomainException("Source name cannot be null or empty");
                
            string normalizedName = sourceName.Trim();
            
            if (!_sourceNames.Contains(normalizedName))
            {
                _sourceNames.Add(normalizedName);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsSourceTrackedEvent(Id, normalizedName));
            }
        }

        public void UntrackSource(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new DomainException("Source name cannot be null or empty");
                
            string normalizedName = sourceName.Trim();
            
            if (_sourceNames.Contains(normalizedName))
            {
                _sourceNames.Remove(normalizedName);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsSourceUntrackedEvent(Id, normalizedName));
            }
        }
        
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Tag cannot be empty");
                
            string normalizedTag = tag.Trim().ToLower();
            
            if (!_tags.Contains(normalizedTag))
            {
                _tags.Add(normalizedTag);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsAggregateTagAddedEvent(Id, normalizedTag));
            }
        }
        
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new DomainException("Tag cannot be empty");
                
            string normalizedTag = tag.Trim().ToLower();
            
            if (_tags.Contains(normalizedTag))
            {
                _tags.Remove(normalizedTag);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsAggregateTagRemovedEvent(Id, normalizedTag));
            }
        }
        
        public void AddCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new DomainException("Category cannot be empty");
                
            string normalizedCategory = category.Trim().ToLower();
            
            if (!_categories.Contains(normalizedCategory))
            {
                _categories.Add(normalizedCategory);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsAggregateCategoryAddedEvent(Id, normalizedCategory));
            }
        }
        
        public void RemoveCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new DomainException("Category cannot be empty");
                
            string normalizedCategory = category.Trim().ToLower();
            
            if (_categories.Contains(normalizedCategory))
            {
                _categories.Remove(normalizedCategory);
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new NewsAggregateCategoryRemovedEvent(Id, normalizedCategory));
            }
        }
        
        public bool ContainsArticle(Guid articleId)
        {
            return _articleIds.Contains(articleId);
        }
        
        public bool ContainsSource(string sourceName)
        {
            return !string.IsNullOrWhiteSpace(sourceName) && 
                   _sourceNames.Contains(sourceName.Trim());
        }
        
        public bool ContainsTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && 
                   _tags.Contains(tag.Trim().ToLower());
        }
        
        public bool ContainsCategory(string category)
        {
            return !string.IsNullOrWhiteSpace(category) && 
                   _categories.Contains(category.Trim().ToLower());
        }
        
        public IReadOnlyCollection<Guid> GetArticleIds()
        {
            return _articleIds.AsReadOnly();
        }
        
        public IReadOnlyCollection<string> GetSourceNames()
        {
            return _sourceNames.AsReadOnly();
        }
        
        public IReadOnlyCollection<string> GetTags()
        {
            return _tags.AsReadOnly();
        }
        
        public IReadOnlyCollection<string> GetCategories()
        {
            return _categories.AsReadOnly();
        }
    }
} 