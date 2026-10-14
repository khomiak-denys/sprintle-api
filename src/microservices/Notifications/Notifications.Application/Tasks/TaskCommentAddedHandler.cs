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
    /// Handles <see cref="TaskCommentAdded"/> events to send email notifications.
    /// Notifies the requester and assignee when a comment is added, excluding the author of the comment.
    /// </summary>
    public sealed partial class TaskCommentAddedHandler : IEventHandler<TaskCommentAdded>
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<TaskCommentAddedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCommentAddedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="notificationRepository">The notification repository for inspecting user preferences.</param>
        /// <param name="logger">The logger instance.</param>
        public TaskCommentAddedHandler(
            IEmailSender emailSender,
            INotificationRepository notificationRepository,
            ILogger<TaskCommentAddedHandler> logger)
        {
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(TaskCommentAdded @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var recipients = ResolveRecipients(@event);
            if (recipients.Count == 0)
            {
                return;
            }

            var subject = TaskCommentAddedTemplate.GetSubject(@event.TaskTitle, @event.WorkspaceName);

            foreach (var (userId, email, name) in recipients)
            {
                var notification = await _notificationRepository.GetByUserIdAndTypeAsync(
                    userId,
                    NotificationType.Email,
                    cancellationToken);

                if (notification is not null && !notification.Enabled)
                {
                    LogNotificationDisabled(userId, @event.TaskId, @event.CommentId);
                    continue;
                }

                var htmlBody = TaskCommentAddedTemplate.Render(
                    @event.TaskTitle,
                    @event.WorkspaceName,
                    @event.CommentContent,
                    @event.CommentAuthorName,
                    name);

                await _emailSender.SendAsync(
                    email,
                    subject,
                    htmlBody,
                    cancellationToken);

                LogNotificationSent(userId, email, @event.TaskId, @event.CommentId);
            }
        }

        private static List<(Guid UserId, string Email, string Name)> ResolveRecipients(TaskCommentAdded @event)
        {
            var recipients = new List<(Guid UserId, string Email, string Name)>();

            // Notify task requester if they are not the comment author
            if (!string.IsNullOrWhiteSpace(@event.CreatedByEmail) && @event.CreatedByUserId != @event.CommentAuthorUserId)
            {
                recipients.Add((@event.CreatedByUserId, @event.CreatedByEmail, @event.CreatedByName));
            }

            // Notify task assignee if assigned, has email, is not the author, and is not already added (when requester == assignee)
            if (@event.AssigneeUserId.HasValue &&
                !string.IsNullOrWhiteSpace(@event.AssigneeEmail) &&
                @event.AssigneeUserId.Value != @event.CommentAuthorUserId &&
                @event.AssigneeUserId.Value != @event.CreatedByUserId)
            {
                recipients.Add((@event.AssigneeUserId.Value, @event.AssigneeEmail, @event.AssigneeName ?? "Assignee"));
            }

            return recipients;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping comment email for user '{UserId}' on task '{TaskId}' for comment '{CommentId}' because email notifications are disabled.")]
        private partial void LogNotificationDisabled(Guid userId, Guid taskId, Guid commentId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Comment email notification sent to user '{UserId}' at '{Email}' for task '{TaskId}', comment '{CommentId}'.")]
        private partial void LogNotificationSent(Guid userId, string email, Guid taskId, Guid commentId);
    }
}
