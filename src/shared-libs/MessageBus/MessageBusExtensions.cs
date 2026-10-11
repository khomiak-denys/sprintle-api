using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    /// <summary>
    /// Provides extension methods for configuring MassTransit with RabbitMQ.
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Registers MassTransit with RabbitMQ using the connection string configured for the application.
        /// </summary>
        /// <param name="services">The service collection to register dependencies into.</param>
        /// <param name="configuration">The configuration provider to retrieve connection settings from.</param>
        /// <param name="configureConsumers">An optional action to configure consumers on the bus.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMessageBus(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator>? configureConsumers = null)
        {
            services.AddMassTransit(busConfig =>
            {
                busConfig.SetKebabCaseEndpointNameFormatter();

                configureConsumers?.Invoke(busConfig);

                busConfig.UsingRabbitMq((context, cfg) =>
                {
                    var connectionString = configuration.GetConnectionString("message-bus");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        cfg.Host(connectionString);
                    }

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
