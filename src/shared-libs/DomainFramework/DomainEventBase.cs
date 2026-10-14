using System;

namespace DomainFramework
{
    /// <summary>
    /// Abstract base record providing default property implementations for <see cref="IDomainEvent"/>.
    /// </summary>
    /// <param name="AggregateId">The unique identifier of the aggregate root.</param>
    public abstract record DomainEventBase(Guid AggregateId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
