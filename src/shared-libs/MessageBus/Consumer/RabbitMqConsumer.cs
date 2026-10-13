using MessageBus.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBus.Consumer
{
    /// <summary>
    /// Abstract background service consumer that manages channel lifecycle, queue topology,
    /// dead-letter configuration, and message dispatching via an <see cref="EventDispatcher"/>.
    /// </summary>
    /// <param name="connectionManager">The RabbitMQ connection manager providing active connections.</param>
    /// <param name="scopeFactory">The service scope factory used to resolve scoped message handlers.</param>
    /// <param name="logger">The logger used to record consumer operations and errors.</param>
    public abstract partial class RabbitMqConsumer(
        RabbitMqConnectionManager connectionManager,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
        : BackgroundService
    {
        private const string DeadLetterExchangeName = "app.events.dlx";

        /// <summary>
        /// Gets the name of the queue to declare and consume from.
        /// </summary>
        protected abstract string QueueName { get; }

        /// <summary>
        /// Gets the routing key pattern used to bind the queue to the topic exchange (e.g., "user.*").
        /// </summary>
        protected abstract string BindingKey { get; }

        /// <summary>
        /// Gets the <see cref="EventDispatcher"/> configured with event mappings for this consumer.
        /// </summary>
        protected abstract EventDispatcher Dispatcher { get; }

        /// <summary>
        /// Gets the name of the main topic exchange. Defaults to "app.events".
        /// </summary>
        protected virtual string ExchangeName => "app.events";

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var channel =
                await connectionManager.Connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 10,
                global: false,
                cancellationToken: stoppingToken);

            // a) Exchange app.events.dlx (Direct, durable)
            await channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                cancellationToken: stoppingToken);

            // b) Queue {QueueName}.dlq (durable) + binding to DLX with routing key {QueueName}.dlq
            var dlqName = $"{QueueName}.dlq";
            await channel.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                queue: dlqName,
                exchange: DeadLetterExchangeName,
                routingKey: dlqName,
                cancellationToken: stoppingToken);

            // Main exchange app.events (Topic, durable)
            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: stoppingToken);

            // c) Main queue (durable) with dead-letter exchange and dead-letter routing key
            var queueArguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = dlqName
            };

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: stoppingToken);

            // d) Binding of main queue to app.events with BindingKey
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: BindingKey,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();

                    await Dispatcher.DispatchAsync(
                        args.RoutingKey,
                        scope.ServiceProvider,
                        args.Body,
                        stoppingToken);

                    await channel.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    LogProcessMessageFailed(ex, QueueName, args.RoutingKey);

                    await channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: CancellationToken.None);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            LogConsumingQueue(QueueName, BindingKey);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process message from queue '{Queue}' with routing key '{RoutingKey}'. Nacked to DLQ.")]
        private partial void LogProcessMessageFailed(Exception exception, string queue, string routingKey);

        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming queue '{Queue}' with binding key '{BindingKey}'")]
        private partial void LogConsumingQueue(string queue, string bindingKey);
    }
}
