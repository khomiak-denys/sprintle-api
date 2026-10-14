using System;
using EmailTemplates;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Tasks
{
    /// <summary>
    /// Handles <see cref="TaskTimeLogged"/> events to send email notifications.
    /// Notifies the task requester when an assignee logs time on their task, unless the requester logged the time themselves.
    /// </summary>
    public sealed partial class TaskTimeLoggedHandler : IEventHandler<TaskTimeLogged>
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<TaskTimeLoggedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskTimeLoggedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="notificationRepository">The notification repository for inspecting user preferences.</param>
        /// <param name="logger">The logger instance.</param>
        public TaskTimeLoggedHandler(
            IEmailSender emailSender,
            INotificationRepository notificationRepository,
            ILogger<TaskTimeLoggedHandler> logger)
        {
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(TaskTimeLogged @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            // Only notify requester, and only if requester didn't log the time themselves
            if (string.IsNullOrWhiteSpace(@event.CreatedByEmail) || @event.CreatedByUserId == @event.LoggedByUserId)
            {
                return;
            }

            var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                @event.CreatedByUserId,
                NotificationType.Email,
                cancellationToken);

            if (notification is not null && !notification.Enabled)
            {
                LogNotificationDisabled(@event.CreatedByUserId, @event.TaskId, @event.TimeLogId);
                return;
            }

            var subject = TaskTimeLoggedTemplate.GetSubject(@event.TaskTitle, @event.MinutesSpent, @event.WorkspaceName);

            var htmlBody = TaskTimeLoggedTemplate.Render(
                @event.TaskTitle,
                @event.WorkspaceName,
                @event.MinutesSpent,
                @event.Description,
                @event.LoggedDate,
                @event.LoggedByUserName,
                @event.CreatedByName);

            await _emailSender.SendAsync(
                @event.CreatedByEmail,
                subject,
                htmlBody,
                cancellationToken);

            LogNotificationSent(@event.CreatedByUserId, @event.CreatedByEmail, @event.TaskId, @event.TimeLogId);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping time logged email for user '{UserId}' on task '{TaskId}', time log '{TimeLogId}' because email notifications are disabled.")]
        private partial void LogNotificationDisabled(Guid userId, Guid taskId, Guid timeLogId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Time logged email notification sent to user '{UserId}' at '{Email}' for task '{TaskId}', time log '{TimeLogId}'.")]
        private partial void LogNotificationSent(Guid userId, string email, Guid taskId, Guid timeLogId);
    }
}
