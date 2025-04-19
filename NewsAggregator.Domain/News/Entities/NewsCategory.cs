using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Events;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Domain.News.Entities
{
    public class NewsCategory : AggregateRoot
    {
        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public NewsProviderType ProviderType { get; private set; }
        public string ProviderSpecificKey { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private NewsCategory() { } // For EF Core

        private NewsCategory(Guid id, string name, string slug, string description, NewsProviderType providerType, string providerSpecificKey = null) : base(id)
        {
            Name = name;
            Slug = slug;
            Description = description ?? string.Empty;
            ProviderType = providerType;
            ProviderSpecificKey = providerSpecificKey ?? slug;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static NewsCategory Create(string name, string slug, string description, NewsProviderType providerType, string providerSpecificKey = null)
        {
            ValidateCategory(name, slug, providerType);

            var category = new NewsCategory(
                Guid.NewGuid(),
                name.Trim(),
                slug.Trim().ToLower(),
                description,
                providerType,
                providerSpecificKey);

            category.AddDomainEvent(new NewsCategoryCreatedEvent(category));

            return category;
        }

        public void Update(string name, string slug, string description, NewsProviderType providerType, string providerSpecificKey = null)
        {
            ValidateCategory(name, slug, providerType);

            Name = name.Trim();
            Slug = slug.Trim().ToLower();
            Description = description ?? string.Empty;
            ProviderType = providerType;
            ProviderSpecificKey = providerSpecificKey ?? slug;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsCategoryUpdatedEvent(this));
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;
                
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsCategoryDeactivatedEvent(this));
        }

        public void Activate()
        {
            if (IsActive)
                return;
                
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new NewsCategoryActivatedEvent(this));
        }
        
        private static void ValidateCategory(string name, string slug, NewsProviderType providerType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Category name cannot be empty");

            if (string.IsNullOrWhiteSpace(slug))
                throw new DomainException("Category slug cannot be empty");

            //if (string.IsNullOrWhiteSpace(providerType))
            //    throw new DomainException("Provider type cannot be empty");
        }
    }
} 