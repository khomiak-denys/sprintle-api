using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Users
{
    /// <summary>
    /// Handles <see cref="UserRegistered"/> event to create a default notification setting for the new user.
    /// </summary>
    public sealed partial class UserRegisteredHandler : IEventHandler<UserRegistered>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserRegisteredHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRegisteredHandler"/> class.
        /// </summary>
        /// <param name="notificationRepository">The notification repository.</param>
        /// <param name="unitOfWork">The unit of work for database persistence.</param>
        /// <param name="logger">The logger instance.</param>
        public UserRegisteredHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<UserRegisteredHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(UserRegistered @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var existing = await _notificationRepository.GetByUserIdAndTypeAsync(
                @event.Id,
                NotificationType.Email,
                cancellationToken);

            if (existing is not null)
            {
                LogNotificationAlreadyExists(@event.Id);
                return;
            }

            var notification = Notification.Create(
                @event.Id,
                @event.Email,
                @event.UserName,
                NotificationType.Email,
                enabled: true);

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogNotificationCreated(@event.Id);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Notification setting for user '{UserId}' already exists.")]
        private partial void LogNotificationAlreadyExists(Guid userId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Created default email notification setting for user '{UserId}'.")]
        private partial void LogNotificationCreated(Guid userId);
    }
}
