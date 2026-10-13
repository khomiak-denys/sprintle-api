namespace Notifications.Domain.Notifications
{
    /// <summary>
    /// Persistence contract for the <see cref="Notification"/> domain entity.
    /// Implemented in <c>Notifications.Infrastructure</c>.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Asynchronously retrieves a notification by its unique identifier.
        /// </summary>
        /// <param name="id">The notification identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching <see cref="Notification"/>, or <c>null</c> if not found.</returns>
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the primary notification record for the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching <see cref="Notification"/>, or <c>null</c> if not found.</returns>
        Task<Notification?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a notification by user identifier and notification type.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="notificationType">The notification channel type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching <see cref="Notification"/>, or <c>null</c> if not found.</returns>
        Task<Notification?> GetByUserIdAndTypeAsync(Guid userId, NotificationType notificationType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves all notification configurations associated with a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A read-only list of notifications for the user.</returns>
        Task<IReadOnlyList<Notification>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves all notification records.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A read-only list of all notifications.</returns>
        Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously stages a new <see cref="Notification"/> entity for persistence upon commit.
        /// </summary>
        /// <param name="notification">The notification entity to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks an existing <see cref="Notification"/> entity as modified for the next save.
        /// </summary>
        /// <param name="notification">The notification entity to update.</param>
        void Update(Notification notification);

        /// <summary>
        /// Stages an existing <see cref="Notification"/> entity for removal upon transaction commit.
        /// </summary>
        /// <param name="notification">The notification entity to delete.</param>
        void Delete(Notification notification);
    }
}
