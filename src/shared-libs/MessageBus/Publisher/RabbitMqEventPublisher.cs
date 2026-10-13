using System.Text.Json;
using MessageBus.Connection;
using Messaging.Abstractions;
using RabbitMQ.Client;

namespace MessageBus.Publisher
{
    /// <summary>
    /// Publishes integration events to RabbitMQ using the configured connection manager and routing convention.
    /// </summary>
    /// <param name="connectionManager">The RabbitMQ connection manager providing active connections.</param>
    /// <param name="routingConvention">The routing convention used to resolve routing keys from event types.</param>
    public sealed class RabbitMqEventPublisher(
        RabbitMqConnectionManager connectionManager,
        IEventRoutingConvention routingConvention)
        : IEventPublisher
    {
        private const string ExchangeName = "app.events";

        /// <inheritdoc />
        public async Task PublishAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default)
            where TEvent : class
        {
            ArgumentNullException.ThrowIfNull(@event);

            var connection = connectionManager.Connection;

            await using var channel =
                await connection.CreateChannelAsync(
                    cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

            var routingKey = routingConvention.GetRoutingKey(typeof(TEvent));

            var body = JsonSerializer.SerializeToUtf8Bytes(@event);

            var properties = new BasicProperties
            {
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
    }
}
