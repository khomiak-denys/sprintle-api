using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence
{
    /// <summary>
    /// Database context for the Notifications microservice.
    /// Implements <see cref="IUnitOfWork"/> to provide transactional persistence boundaries.
    /// </summary>
    public class NotificationsDbContext : DbContext, IUnitOfWork
    {
        /// <summary>Gets the database set of notifications.</summary>
        public DbSet<Notification> Notifications => Set<Notification>();

        /// <summary>
        /// Initializes a new instance of <see cref="NotificationsDbContext"/> with the specified options.
        /// </summary>
        /// <param name="options">DbContext configuration options.</param>
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        }
    }
}
