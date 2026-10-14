using MessageBus;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tags;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using Workspaces.Infrastructure.Clients;
using Workspaces.Infrastructure.Persistence;
using Workspaces.Infrastructure.Persistence.Repositories;

namespace Workspaces.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers infrastructure services, persistence, and HTTP clients into the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            services.AddDbContext<WorkspacesDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("WorkspacesDb") ?? configuration.GetConnectionString("TasksDb")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WorkspacesDbContext>());
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();

            services.AddHttpClient<IUsersApiClient, UsersApiClient>(client =>
            {
                client.BaseAddress = new Uri(configuration["UsersApi:BaseUrl"]
                    ?? throw new InvalidOperationException("UsersApi URL is not configured."));
            });

            services.AddMessageBus(configuration);

            return services;
        }
    }
}
