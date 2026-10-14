using MessageBus.Connection;
using MessageBus.Consumer;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Consumers
{
    /// <summary>
    /// Background consumer for task-related events published to the "notifications.tasks" queue.
    /// </summary>
    public sealed class TasksConsumer : RabbitMqConsumer
    {
        /// <inheritdoc />
        protected override string QueueName => "notifications.tasks";

        /// <inheritdoc />
        protected override string BindingKey => "task.#";

        /// <inheritdoc />
        protected override EventDispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TasksConsumer"/> class.
        /// Configures the <see cref="EventDispatcher"/> with mappings for <see cref="TaskAssigned"/>.
        /// </summary>
        /// <param name="connectionManager">The RabbitMQ connection manager providing active connections.</param>
        /// <param name="scopeFactory">The service scope factory used to resolve scoped handlers.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="routingConvention">The routing convention used to derive event routing keys.</param>
        public TasksConsumer(
            RabbitMqConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger<TasksConsumer> logger,
            IEventRoutingConvention routingConvention)
            : base(connectionManager, scopeFactory, logger)
        {
            Dispatcher = new EventDispatcher(routingConvention)
                .Map<TaskAssigned>()
                .Map<TaskStatusChanged>()
                .Map<TaskCommentAdded>()
                .Map<TaskTimeLogged>();
        }
    }
}
