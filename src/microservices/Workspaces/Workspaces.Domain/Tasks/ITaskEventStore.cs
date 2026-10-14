using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainFramework;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Contract for persisting and retrieving domain event streams for task aggregates.
    /// </summary>
    public interface ITaskEventStore
    {
        /// <summary>
        /// Retrieves the complete, chronologically ordered sequence of domain events for a given task aggregate.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The read-only collection of domain events for the specified task stream.</returns>
        Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends a collection of newly emitted domain events to the task aggregate event stream.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task aggregate.</param>
        /// <param name="expectedVersion">The expected stream version prior to appending, used for optimistic concurrency control.</param>
        /// <param name="events">The domain events to append.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AppendEventsAsync(
            Guid taskId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken = default);
    }
}
