using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="INotificationRepository"/>.
    /// </summary>
    public sealed class NotificationRepository : INotificationRepository
    {
        private readonly NotificationsDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="NotificationRepository"/>.
        /// </summary>
        /// <param name="context">The notifications database context.</param>
        public NotificationRepository(NotificationsDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Notification?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Notification?> GetByUserIdAndTypeAsync(
            Guid userId,
            NotificationType notificationType,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == userId && n.NotificationType == notificationType, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsEnabledAsync(
            Guid userId,
            NotificationType notificationType = NotificationType.Email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AnyAsync(
                    n => n.UserId == userId && n.NotificationType == notificationType && n.Enabled,
                    cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Notification>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Notifications.AddAsync(notification, cancellationToken);
        }

        /// <inheritdoc/>
        public void Update(Notification notification)
        {
            _context.Notifications.Update(notification);
        }

        /// <inheritdoc/>
        public void Delete(Notification notification)
        {
            _context.Notifications.Remove(notification);
        }
    }
}
