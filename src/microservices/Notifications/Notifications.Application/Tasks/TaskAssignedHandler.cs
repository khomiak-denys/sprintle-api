using System;
using EmailTemplates;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Tasks
{
    /// <summary>
    /// Handles <see cref="TaskAssigned"/> event to send task assignment notification emails to assigned users.
    /// </summary>
    public sealed partial class TaskAssignedHandler : IEventHandler<TaskAssigned>
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly IOptions<FrontendOptions> _frontendOptions;
        private readonly ILogger<TaskAssignedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAssignedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="notificationRepository">The notification repository for inspecting user preferences.</param>
        /// <param name="frontendOptions">The frontend options containing application addresses.</param>
        /// <param name="logger">The logger instance.</param>
        public TaskAssignedHandler(
            IEmailSender emailSender,
            INotificationRepository notificationRepository,
            IOptions<FrontendOptions> frontendOptions,
            ILogger<TaskAssignedHandler> logger)
        {
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _frontendOptions = frontendOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(TaskAssigned @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                @event.AssigneeUserId,
                NotificationType.Email,
                cancellationToken);

            if (notification is not null && !notification.Enabled)
            {
                LogNotificationDisabled(@event.AssigneeUserId, @event.TaskId);
                return;
            }

            var baseUrl = _frontendOptions.Value.DashboardPageAddress;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:4200/dashboard";
            }

            var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var taskUrl = $"{baseUrl}{separator}workspaceId={@event.WorkspaceId}&taskId={@event.TaskId}";

            var htmlBody = TaskAssignedTemplate.Render(
                @event.TaskTitle,
                @event.TaskDescription,
                @event.WorkspaceName,
                @event.AssignedByName,
                @event.Priority,
                @event.Deadline,
                taskUrl,
                @event.AssigneeName);

            var subject = TaskAssignedTemplate.GetSubject(@event.TaskTitle, @event.WorkspaceName);

            await _emailSender.SendAsync(
                @event.AssigneeEmail,
                subject,
                htmlBody,
                cancellationToken);

            LogNotificationSent(@event.AssigneeUserId, @event.AssigneeEmail, @event.TaskId);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping task assignment email for user '{UserId}' on task '{TaskId}' because email notifications are disabled.")]
        private partial void LogNotificationDisabled(Guid userId, Guid taskId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Task assignment email notification sent to user '{UserId}' at '{Email}' for task '{TaskId}'.")]
        private partial void LogNotificationSent(Guid userId, string email, Guid taskId);
    }
}
