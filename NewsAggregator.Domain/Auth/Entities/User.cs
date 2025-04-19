using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.Auth.ValueObjects;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.Auth.Entities
{
    public class User : AggregateRoot
    {
        public string Email { get; private set; }
        public string Name { get; private set; }
        public string Picture { get; private set; }
        private List<UserRole> _roles = new List<UserRole>();
        public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public string ExternalProviderId { get; private set; }
        public string ExternalProviderName { get; private set; }

        // For EF Core
        private User() { }

        private User(
            Guid id,
            string email,
            string name,
            string picture,
            string externalProviderId = null,
            string externalProviderName = null) : base(id)
        {
            Email = email;
            Name = name;
            Picture = picture;
            ExternalProviderId = externalProviderId;
            ExternalProviderName = externalProviderName;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static User Create(
            string email,
            string name,
            string picture = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email cannot be empty");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Name cannot be empty");

            return new User(
                Guid.NewGuid(),
                email.ToLowerInvariant(),
                name,
                picture);
        }

        public static User CreateFromExternalProvider(
            string email,
            string name,
            string externalProviderId,
            string externalProviderName,
            string picture = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email cannot be empty");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Name cannot be empty");

            if (string.IsNullOrWhiteSpace(externalProviderId))
                throw new DomainException("External provider ID cannot be empty");

            if (string.IsNullOrWhiteSpace(externalProviderName))
                throw new DomainException("External provider name cannot be empty");

            return new User(
                Guid.NewGuid(),
                email.ToLowerInvariant(),
                name,
                picture,
                externalProviderId,
                externalProviderName);
        }

        public void UpdateProfile(string name, string picture)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name;

            if (picture != null)
                Picture = picture;

            UpdatedAt = DateTime.UtcNow;
        }

        public void AddRole(string roleName)
        {
            if (_roles.Exists(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                return;

            _roles.Add(UserRole.Create(roleName));
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveRole(string roleName)
        {
            _roles.RemoveAll(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
} 