using FluentValidation;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Tasks;
using Notifications.Application.Users;
using Notifications.Application.Workspaces;

namespace Notifications.Application
{
    /// <summary>
    /// Extension methods that register all Application-layer services into the DI container.
    /// Called from the API composition root (<c>Notifications.API/Program.cs</c>).
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers MediatR handlers, pipeline behaviors, FluentValidation validators,
        /// and messaging event handlers from the <c>Notifications.Application</c> assembly.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            services.AddScoped<IEventHandler<UserRegistered>, UserRegisteredHandler>();
            services.AddScoped<IEventHandler<UserEmailConfirmed>, UserEmailConfirmedHandler>();
            services.AddScoped<IEventHandler<UserEmailConfirmationRequested>, UserEmailConfirmationRequestedHandler>();
            services.AddScoped<IEventHandler<WorkspaceMemberAdded>, WorkspaceMemberAddedHandler>();
            services.AddScoped<IEventHandler<TaskAssigned>, TaskAssignedHandler>();
            services.AddScoped<IEventHandler<TaskStatusChanged>, TaskStatusChangedHandler>();
            services.AddScoped<IEventHandler<TaskCommentAdded>, TaskCommentAddedHandler>();

            return services;
        }
    }
}
