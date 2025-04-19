using System;
using System.Collections.Generic;
using System.Linq;

namespace NewsAggregator.Domain.Common
{
    public abstract class AggregateRoot : Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        protected AggregateRoot() : base() { }

        protected AggregateRoot(Guid id) : base(id) { }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new DomainException("Domain event cannot be null");
                
            _domainEvents.Add(domainEvent);
        }
        
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new DomainException("Domain event cannot be null");
                
            _domainEvents.Remove(domainEvent);
        }
        
        protected void RemoveDomainEvents<T>() where T : IDomainEvent
        {
            var eventsToRemove = _domainEvents.OfType<T>().ToList();
            foreach (var evt in eventsToRemove)
            {
                _domainEvents.Remove(evt);
            }
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
} 