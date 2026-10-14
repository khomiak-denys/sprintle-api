using MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Consumers;
using Notifications.Infrastructure.Emails;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Resend;

namespace Notifications.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers persistence services, database contexts, repositories, message bus connection manager,
        /// background consumers, and email services into the service collection.
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
            services.AddHostedService<WorkspacesConsumer>();
            services.AddHostedService<TasksConsumer>();

            services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
            services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
            services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));

            var emailProvider = configuration["EmailProvider"];
            var apiToken = configuration.GetSection(ResendOptions.SectionName)["ApiToken"];

            var useResend = string.Equals(emailProvider, "Resend", StringComparison.OrdinalIgnoreCase)
                || (!string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(apiToken)
                    && !apiToken.Contains("your_resend_api_token", StringComparison.OrdinalIgnoreCase));

            if (useResend)
            {
                services.AddResend(options =>
                {
                    options.ApiToken = apiToken ?? string.Empty;
                });
                services.AddTransient<IEmailSender, ResendEmailSender>();
            }
            else
            {
                services.AddTransient<IEmailSender, SmtpEmailSender>();
            }

            return services;
        }
    }
}
