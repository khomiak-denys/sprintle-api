namespace Messaging.Abstractions
{
    /// <summary>
    /// Defines a contract for publishing events or messages to a message bus.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event message asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to publish.</typeparam>
        /// <param name="event">The event payload to publish.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        Task PublishAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default)
            where TEvent : class;
    }
}
