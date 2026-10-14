using System;

namespace DomainFramework
{
    /// <summary>
    /// Represents an immutable domain event emitted by an aggregate root.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the unique identifier for this event instance.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Gets the unique identifier of the aggregate root that emitted this event.
        /// </summary>
        Guid AggregateId { get; }

        /// <summary>
        /// Gets the UTC timestamp when the domain event occurred.
        /// </summary>
        DateTime OccurredAt { get; }
    }
}
