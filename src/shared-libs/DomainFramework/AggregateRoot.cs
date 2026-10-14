using System;
using System.Collections.Generic;

namespace DomainFramework
{
    /// <summary>
    /// Base class for domain aggregate roots supporting event sourcing and domain event emission.
    /// Manages uncommitted events and state rehydration from event streams.
    /// </summary>
    public abstract class AggregateRoot : EntityBase
    {
        private readonly List<IDomainEvent> _uncommittedEvents = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot"/> class with a new unique identifier.
        /// </summary>
        protected AggregateRoot() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot"/> class with a specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the aggregate.</param>
        protected AggregateRoot(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Gets the current stream version of the aggregate root (the count of applied events).
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// Gets the read-only collection of uncommitted domain events produced during the current operation.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        /// <summary>
        /// Clears all uncommitted domain events once they have been successfully persisted.
        /// </summary>
        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        /// <summary>
        /// Applies the specified domain event to mutate aggregate state, records it into the uncommitted events list,
        /// increments the version, and updates the aggregate timestamp.
        /// </summary>
        /// <param name="event">The domain event to apply and record.</param>
        protected void RaiseEvent(IDomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);
            Apply(@event);
            _uncommittedEvents.Add(@event);
            Version++;
            OnModify();
        }

        /// <summary>
        /// Rehydrates the aggregate state from an ordered stream of historical domain events.
        /// </summary>
        /// <param name="events">The collection of historical domain events in chronological order.</param>
        public void Rehydrate(IEnumerable<IDomainEvent> events)
        {
            ArgumentNullException.ThrowIfNull(events);

            foreach (var @event in events)
            {
                Apply(@event);
                Version++;
            }
        }

        /// <summary>
        /// Dispatches the state transition corresponding to the specified domain event.
        /// Must be implemented by derived aggregate roots.
        /// </summary>
        /// <param name="event">The domain event to apply.</param>
        protected abstract void Apply(IDomainEvent @event);
    }
}
