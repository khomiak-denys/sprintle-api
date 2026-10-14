using System;
using System.Collections.Generic;
using EmailTemplates;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Tasks
{
    /// <summary>
    /// Handles <see cref="TaskStatusChanged"/> events to send email notifications.
    /// Specifically notifies the requester when a task is completed/submitted for review,
    /// and notifies the assignee when a task is approved, cancelled, or closed.
    /// </summary>
    public sealed partial class TaskStatusChangedHandler : IEventHandler<TaskStatusChanged>
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<TaskStatusChangedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskStatusChangedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="notificationRepository">The notification repository for inspecting user preferences.</param>
        /// <param name="logger">The logger instance.</param>
        public TaskStatusChangedHandler(
            IEmailSender emailSender,
            INotificationRepository notificationRepository,
            ILogger<TaskStatusChangedHandler> logger)
        {
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(TaskStatusChanged @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var recipients = ResolveRecipients(@event);
            if (recipients.Count == 0)
            {
                return;
            }

            var subject = TaskStatusChangedTemplate.GetSubject(@event.TaskTitle, @event.NewStatus, @event.WorkspaceName);

            foreach (var (userId, email, name) in recipients)
            {
                var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                    userId,
                    NotificationType.Email,
                    cancellationToken);

                if (notification is not null && !notification.Enabled)
                {
                    LogNotificationDisabled(userId, @event.TaskId, @event.NewStatus);
                    continue;
                }

                var htmlBody = TaskStatusChangedTemplate.Render(
                    @event.TaskTitle,
                    @event.WorkspaceName,
                    @event.OldStatus,
                    @event.NewStatus,
                    @event.ChangedByUserName,
                    name);

                await _emailSender.SendAsync(
                    email,
                    subject,
                    htmlBody,
                    cancellationToken);

                LogNotificationSent(userId, email, @event.TaskId, @event.NewStatus);
            }
        }

        private static List<(Guid UserId, string Email, string Name)> ResolveRecipients(TaskStatusChanged @event)
        {
            var recipients = new List<(Guid UserId, string Email, string Name)>();

            if (string.Equals(@event.NewStatus, "InReview", StringComparison.OrdinalIgnoreCase))
            {
                // Completed & submitted for review -> notify requester (creator)
                if (!string.IsNullOrWhiteSpace(@event.CreatedByEmail) && @event.CreatedByUserId != @event.ChangedByUserId)
                {
                    recipients.Add((@event.CreatedByUserId, @event.CreatedByEmail, @event.CreatedByName));
                }
            }
            else if (string.Equals(@event.NewStatus, "Done", StringComparison.OrdinalIgnoreCase))
            {
                // Approved -> notify assignee
                if (@event.AssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(@event.AssigneeEmail) && @event.AssigneeUserId.Value != @event.ChangedByUserId)
                {
                    recipients.Add((@event.AssigneeUserId.Value, @event.AssigneeEmail, @event.AssigneeName ?? "Assignee"));
                }
            }
            else if (string.Equals(@event.NewStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Cancelled -> only notify assignee, if task unassigned don't send email
                if (@event.AssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(@event.AssigneeEmail) && @event.AssigneeUserId.Value != @event.ChangedByUserId)
                {
                    recipients.Add((@event.AssigneeUserId.Value, @event.AssigneeEmail, @event.AssigneeName ?? "Assignee"));
                }
            }
            else if (string.Equals(@event.NewStatus, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                // Closed -> only notify assignee, if task unassigned don't send email
                if (@event.AssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(@event.AssigneeEmail) && @event.AssigneeUserId.Value != @event.ChangedByUserId)
                {
                    recipients.Add((@event.AssigneeUserId.Value, @event.AssigneeEmail, @event.AssigneeName ?? "Assignee"));
                }
            }
            else
            {
                // Other transitions
                if (@event.ChangedByUserId == @event.CreatedByUserId && @event.AssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(@event.AssigneeEmail))
                {
                    recipients.Add((@event.AssigneeUserId.Value, @event.AssigneeEmail, @event.AssigneeName ?? "Assignee"));
                }
                else if (!string.IsNullOrWhiteSpace(@event.CreatedByEmail) && @event.CreatedByUserId != @event.ChangedByUserId)
                {
                    recipients.Add((@event.CreatedByUserId, @event.CreatedByEmail, @event.CreatedByName));
                }
            }

            return recipients;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping task status email for user '{UserId}' on task '{TaskId}' for status '{Status}' because email notifications are disabled.")]
        private partial void LogNotificationDisabled(Guid userId, Guid taskId, string status);

        [LoggerMessage(Level = LogLevel.Information, Message = "Task status email notification sent to user '{UserId}' at '{Email}' for task '{TaskId}' with new status '{Status}'.")]
        private partial void LogNotificationSent(Guid userId, string email, Guid taskId, string status);
    }
}
