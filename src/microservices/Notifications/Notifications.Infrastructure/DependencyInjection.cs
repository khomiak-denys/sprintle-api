using MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Consumers;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;

namespace Notifications.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers persistence services, database contexts, repositories, message bus connection manager,
        /// and background consumers into the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("NotificationsDb")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
            services.AddScoped<INotificationRepository, NotificationRepository>();

            services.AddMessageBus(configuration);
            services.AddHostedService<UsersConsumer>();

            return services;
        }
    }
}
