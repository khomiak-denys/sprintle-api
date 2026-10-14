using EmailTemplates;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Workspaces
{
    /// <summary>
    /// Handles <see cref="WorkspaceMemberAdded"/> event to send workspace membership notification emails to newly added members.
    /// </summary>
    public sealed partial class WorkspaceMemberAddedHandler : IEventHandler<WorkspaceMemberAdded>
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<WorkspaceMemberAddedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceMemberAddedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="notificationRepository">The notification repository for inspecting user preferences.</param>
        /// <param name="logger">The logger instance.</param>
        public WorkspaceMemberAddedHandler(
            IEmailSender emailSender,
            INotificationRepository notificationRepository,
            ILogger<WorkspaceMemberAddedHandler> logger)
        {
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(WorkspaceMemberAdded @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                @event.MemberUserId,
                NotificationType.Email,
                cancellationToken);

            if (notification is not null && !notification.Enabled)
            {
                LogNotificationDisabled(@event.MemberUserId, @event.WorkspaceId);
                return;
            }

            var htmlBody = WorkspaceMemberAddedTemplate.Render(
                @event.WorkspaceName,
                @event.AddedByName,
                @event.MemberName);

            var subject = WorkspaceMemberAddedTemplate.GetSubject(@event.WorkspaceName);

            await _emailSender.SendAsync(
                @event.MemberEmail,
                subject,
                htmlBody,
                cancellationToken);

            LogNotificationSent(@event.MemberUserId, @event.MemberEmail, @event.WorkspaceId);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping workspace membership email for user '{UserId}' in workspace '{WorkspaceId}' because email notifications are disabled.")]
        private partial void LogNotificationDisabled(Guid userId, Guid workspaceId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Workspace membership email notification sent to user '{UserId}' at '{Email}' for workspace '{WorkspaceId}'.")]
        private partial void LogNotificationSent(Guid userId, string email, Guid workspaceId);
    }
}
