using System.Text.Json;
using Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Consumer
{
    /// <summary>
    /// Dispatches raw message payloads to their corresponding strongly-typed event handlers based on routing keys.
    /// </summary>
    public sealed class EventDispatcher
    {
        private readonly IEventRoutingConvention _routingConvention;
        private readonly Dictionary<string, Func<IServiceProvider, ReadOnlyMemory<byte>, CancellationToken, Task>> _handlers =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDispatcher"/> class with the specified routing convention.
        /// </summary>
        /// <param name="routingConvention">The event routing convention used to derive routing keys from event types.</param>
        public EventDispatcher(IEventRoutingConvention routingConvention)
        {
            _routingConvention = routingConvention ?? throw new ArgumentNullException(nameof(routingConvention));
        }

        /// <summary>
        /// Registers a mapping for event type <typeparamref name="TEvent"/> using the configured routing convention.
        /// </summary>
        /// <typeparam name="TEvent">The event message type to map.</typeparam>
        /// <returns>The current <see cref="EventDispatcher"/> instance for method chaining.</returns>
        public EventDispatcher Map<TEvent>() where TEvent : class
        {
            var routingKey = _routingConvention.GetRoutingKey(typeof(TEvent));

            _handlers[routingKey] = async (serviceProvider, body, cancellationToken) =>
            {
                var @event = JsonSerializer.Deserialize<TEvent>(body.Span)
                    ?? throw new JsonException($"Deserialized message payload for '{typeof(TEvent).Name}' is null.");

                var handler = serviceProvider.GetRequiredService<IEventHandler<TEvent>>();

                await handler.HandleAsync(@event, cancellationToken);
            };

            return this;
        }

        /// <summary>
        /// Dispatches the message payload to the registered handler delegate matching the routing key.
        /// </summary>
        /// <param name="routingKey">The routing key of the received message.</param>
        /// <param name="sp">The service provider used to resolve scoped handlers.</param>
        /// <param name="body">The raw message body.</param>
        /// <param name="ct">Cancellation token for asynchronous processing.</param>
        /// <returns>A task representing the asynchronous dispatch operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="routingKey"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sp"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no handler delegate is mapped for the routing key.</exception>
        /// <exception cref="JsonException">Thrown when deserialization yields a null event.</exception>
        public async Task DispatchAsync(
            string routingKey,
            IServiceProvider sp,
            ReadOnlyMemory<byte> body,
            CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
            ArgumentNullException.ThrowIfNull(sp);

            if (!_handlers.TryGetValue(routingKey, out var handlerDelegate))
            {
                throw new InvalidOperationException($"No handler registered for routing key '{routingKey}'.");
            }

            await handlerDelegate(sp, body, ct);
        }
    }
}
