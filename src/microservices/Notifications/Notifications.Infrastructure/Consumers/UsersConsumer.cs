using MessageBus.Connection;
using MessageBus.Consumer;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Consumers
{
    /// <summary>
    /// Background consumer for user-related events published to the "notifications.users" queue.
    /// </summary>
    public sealed class UsersConsumer : RabbitMqConsumer
    {
        /// <inheritdoc />
        protected override string QueueName => "notifications.users";

        /// <inheritdoc />
        protected override string BindingKey => "user.*";

        /// <inheritdoc />
        protected override EventDispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersConsumer"/> class.
        /// Configures the <see cref="EventDispatcher"/> with mappings for <see cref="UserRegistered"/> and <see cref="UserEmailConfirmed"/>.
        /// </summary>
        /// <param name="connectionManager">The RabbitMQ connection manager providing active connections.</param>
        /// <param name="scopeFactory">The service scope factory used to resolve scoped handlers.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="routingConvention">The routing convention used to derive event routing keys.</param>
        public UsersConsumer(
            RabbitMqConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger<UsersConsumer> logger,
            IEventRoutingConvention routingConvention)
            : base(connectionManager, scopeFactory, logger)
        {
            Dispatcher = new EventDispatcher(routingConvention)
                .Map<UserRegistered>()
                .Map<UserEmailConfirmed>();
        }
    }
}
