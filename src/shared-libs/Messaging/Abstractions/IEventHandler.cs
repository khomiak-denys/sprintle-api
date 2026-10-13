using System.Diagnostics.CodeAnalysis;

namespace Messaging.Abstractions
{
    /// <summary>
    /// Defines a contract for handling events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to handle.</typeparam>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "IEventHandler is standard domain terminology for messaging event handlers.")]
    public interface IEventHandler<in TEvent>
        where TEvent : class
    {
        /// <summary>
        /// Handles the incoming event asynchronously.
        /// </summary>
        /// <param name="event">The event payload to handle.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous handling operation.</returns>
        Task HandleAsync(
            TEvent @event,
            CancellationToken cancellationToken = default);
    }
}
