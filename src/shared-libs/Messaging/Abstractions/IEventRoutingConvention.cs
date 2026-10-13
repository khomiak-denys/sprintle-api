namespace Messaging.Abstractions
{
    /// <summary>
    /// Defines a convention for resolving message bus routing keys from event types.
    /// </summary>
    public interface IEventRoutingConvention
    {
        /// <summary>
        /// Gets the routing key corresponding to the specified event type.
        /// </summary>
        /// <param name="eventType">The runtime type of the event.</param>
        /// <returns>The routing key string for the event.</returns>
        string GetRoutingKey(Type eventType);
    }
}
