using MessageBus.Connection;
using MessageBus.Convention;
using MessageBus.Publisher;
using Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus
{
    /// <summary>
    /// Provides extension methods for configuring RabbitMQ message bus.
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Registers the RabbitMQ connection manager, hosted service, routing convention, and event publisher.
        /// </summary>
        /// <param name="services">The service collection to register dependencies into.</param>
        /// <param name="configuration">The configuration provider to retrieve connection settings from.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMessageBus(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<RabbitMqConnectionManager>();

            services.AddSingleton<IHostedService>(
                sp => sp.GetRequiredService<RabbitMqConnectionManager>());

            services.AddSingleton<IEventRoutingConvention, RabbitMqRoutingConvention>();

            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

            return services;
        }
    }
}
