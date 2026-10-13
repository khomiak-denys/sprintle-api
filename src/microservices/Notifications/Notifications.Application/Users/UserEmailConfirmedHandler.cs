using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Users
{
    /// <summary>
    /// Handles <see cref="UserEmailConfirmed"/> event to update and enable email notifications for the user.
    /// </summary>
    public sealed partial class UserEmailConfirmedHandler : IEventHandler<UserEmailConfirmed>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserEmailConfirmedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEmailConfirmedHandler"/> class.
        /// </summary>
        /// <param name="notificationRepository">The notification repository.</param>
        /// <param name="unitOfWork">The unit of work for database persistence.</param>
        /// <param name="logger">The logger instance.</param>
        public UserEmailConfirmedHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<UserEmailConfirmedHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(UserEmailConfirmed @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                @event.Id,
                NotificationType.Email,
                cancellationToken);

            if (notification is null)
            {
                LogNotificationNotFound(@event.Id);
                return;
            }

            notification.UpdateEmail(@event.Email);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogNotificationUpdated(@event.Id);
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Notification setting for user '{UserId}' not found on email confirmation.")]
        private partial void LogNotificationNotFound(Guid userId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Updated and enabled email notifications for user '{UserId}'.")]
        private partial void LogNotificationUpdated(Guid userId);
    }
}
