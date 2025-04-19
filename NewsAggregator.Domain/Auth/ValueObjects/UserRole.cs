using System;
using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.Auth.ValueObjects
{
    public class UserRole : ValueObject
    {
        public string Name { get; private set; }
        public DateTime AssignedAt { get; private set; }

        private UserRole(string name)
        {
            Name = name;
            AssignedAt = DateTime.UtcNow;
        }

        public static UserRole Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Role name cannot be empty");

            return new UserRole(name.ToUpperInvariant());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
} 