using MessageBus.Connection;
using MessageBus.Consumer;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Consumers
{
    /// <summary>
    /// Background consumer for workspace-related events published to the "notifications.workspaces" queue.
    /// </summary>
    public sealed class WorkspacesConsumer : RabbitMqConsumer
    {
        /// <inheritdoc />
        protected override string QueueName => "notifications.workspaces";

        /// <inheritdoc />
        protected override string BindingKey => "workspace.#";

        /// <inheritdoc />
        protected override EventDispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspacesConsumer"/> class.
        /// Configures the <see cref="EventDispatcher"/> with mappings for <see cref="WorkspaceMemberAdded"/>.
        /// </summary>
        /// <param name="connectionManager">The RabbitMQ connection manager providing active connections.</param>
        /// <param name="scopeFactory">The service scope factory used to resolve scoped handlers.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="routingConvention">The routing convention used to derive event routing keys.</param>
        public WorkspacesConsumer(
            RabbitMqConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger<WorkspacesConsumer> logger,
            IEventRoutingConvention routingConvention)
            : base(connectionManager, scopeFactory, logger)
        {
            Dispatcher = new EventDispatcher(routingConvention)
                .Map<WorkspaceMemberAdded>();
        }
    }
}
