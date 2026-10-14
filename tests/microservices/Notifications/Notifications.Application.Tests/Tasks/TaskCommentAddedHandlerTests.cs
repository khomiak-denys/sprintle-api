using EmailTemplates;
using FluentAssertions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Application.Abstractions;
using Notifications.Application.Tasks;
using Notifications.Domain.Notifications;
using NSubstitute;
using Xunit;

namespace Notifications.Application.Tests.Tasks
{
    public class TaskCommentAddedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly TaskCommentAddedHandler _handler;

        public TaskCommentAddedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _notificationRepository = Substitute.For<INotificationRepository>();
            _handler = new TaskCommentAddedHandler(
                _emailSender,
                _notificationRepository,
                NullLogger<TaskCommentAddedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailToBothRequesterAndAssignee_When_CommentAddedByThirdParty()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var thirdPartyId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";
            var assigneeEmail = "assignee@example.com";

            var requesterNotification = Notification.Create(requesterId, requesterEmail, "requester", NotificationType.Email, enabled: true);
            var assigneeNotification = Notification.Create(assigneeId, assigneeEmail, "assignee", NotificationType.Email, enabled: true);

            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(requesterNotification);
            _notificationRepository.GetByUserIdAndTypeAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(assigneeNotification);

            var @event = new TaskCommentAdded(
                taskId,
                "API Optimization",
                workspaceId,
                "Core Platform",
                Guid.NewGuid(),
                "Please check the latest benchmark results.",
                thirdPartyId,
                "admin_dave",
                requesterId,
                requesterEmail,
                "Requester Name",
                assigneeId,
                assigneeEmail,
                "Assignee Name");

            var sentEmails = new List<(string To, string Subject, string Body)>();
            _emailSender.SendAsync(
                Arg.Do<string>(to => sentEmails.Add((to, "", ""))),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(2).SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            sentEmails.Select(e => e.To).Should().BeEquivalentTo(new[] { requesterEmail, assigneeEmail });
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailOnlyToAssignee_When_CommentAddedByRequester()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";
            var assigneeEmail = "assignee@example.com";

            var assigneeNotification = Notification.Create(assigneeId, assigneeEmail, "assignee", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(assigneeNotification);

            var @event = new TaskCommentAdded(
                taskId,
                "API Optimization",
                workspaceId,
                "Core Platform",
                Guid.NewGuid(),
                "Here are more details about the requirement.",
                requesterId,
                "requester_user",
                requesterId,
                requesterEmail,
                "Requester Name",
                assigneeId,
                assigneeEmail,
                "Assignee Name");

            string? capturedTo = null;
            string? capturedSubject = null;
            string? capturedBody = null;

            _emailSender.SendAsync(
                Arg.Do<string>(to => capturedTo = to),
                Arg.Do<string>(sub => capturedSubject = sub),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(1).SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            capturedTo.Should().Be(assigneeEmail);
            capturedSubject.Should().Be(TaskCommentAddedTemplate.GetSubject("API Optimization", "Core Platform"));
            capturedBody.Should().Contain("Here are more details about the requirement.");
            capturedBody.Should().Contain("requester_user");
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailOnlyToRequester_When_CommentAddedByAssignee()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";
            var assigneeEmail = "assignee@example.com";

            var requesterNotification = Notification.Create(requesterId, requesterEmail, "requester", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(requesterNotification);

            var @event = new TaskCommentAdded(
                taskId,
                "API Optimization",
                workspaceId,
                "Core Platform",
                Guid.NewGuid(),
                "I have completed the changes, ready for testing.",
                assigneeId,
                "assignee_dev",
                requesterId,
                requesterEmail,
                "Requester Name",
                assigneeId,
                assigneeEmail,
                "Assignee Name");

            string? capturedTo = null;
            string? capturedSubject = null;
            string? capturedBody = null;

            _emailSender.SendAsync(
                Arg.Do<string>(to => capturedTo = to),
                Arg.Do<string>(sub => capturedSubject = sub),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(1).SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            capturedTo.Should().Be(requesterEmail);
            capturedSubject.Should().Be(TaskCommentAddedTemplate.GetSubject("API Optimization", "Core Platform"));
            capturedBody.Should().Contain("I have completed the changes, ready for testing.");
            capturedBody.Should().Contain("assignee_dev");
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailOnlyToRequester_When_CommentAddedByThirdPartyAndTaskUnassigned()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var thirdPartyId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";

            var requesterNotification = Notification.Create(requesterId, requesterEmail, "requester", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(requesterNotification);

            var @event = new TaskCommentAdded(
                taskId,
                "Unassigned Feature",
                workspaceId,
                "Core Platform",
                Guid.NewGuid(),
                "A note on an unassigned task.",
                thirdPartyId,
                "manager_sarah",
                requesterId,
                requesterEmail,
                "Requester Name",
                null,
                null,
                null);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(1).SendAsync(
                requesterEmail,
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_NotSendEmail_When_CommentAddedByRequesterAndTaskUnassigned()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var @event = new TaskCommentAdded(
                Guid.NewGuid(),
                "Unassigned Feature",
                Guid.NewGuid(),
                "Core Platform",
                Guid.NewGuid(),
                "Self note by requester.",
                requesterId,
                "requester",
                requesterId,
                "requester@example.com",
                "Requester Name",
                null,
                null,
                null);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.DidNotReceive().SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_NotSendEmail_When_RequesterIsAssigneeAndCommentsThemselves()
        {
            // Arrange
            var sameUserId = Guid.NewGuid();
            var @event = new TaskCommentAdded(
                Guid.NewGuid(),
                "Self Assigned Task",
                Guid.NewGuid(),
                "Core Platform",
                Guid.NewGuid(),
                "Self note.",
                sameUserId,
                "same_user",
                sameUserId,
                "same@example.com",
                "Same User",
                sameUserId,
                "same@example.com",
                "Same User");

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.DidNotReceive().SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_SendOnlyOneEmail_When_RequesterIsAssigneeAndThirdPartyComments()
        {
            // Arrange
            var sameUserId = Guid.NewGuid();
            var thirdPartyId = Guid.NewGuid();
            var userEmail = "same@example.com";

            var notification = Notification.Create(sameUserId, userEmail, "same_user", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(sameUserId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskCommentAdded(
                Guid.NewGuid(),
                "Self Assigned Task",
                Guid.NewGuid(),
                "Core Platform",
                Guid.NewGuid(),
                "Feedback from lead.",
                thirdPartyId,
                "lead_user",
                sameUserId,
                userEmail,
                "Same User",
                sameUserId,
                userEmail,
                "Same User");

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(1).SendAsync(
                userEmail,
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationDisabledForUser()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var thirdPartyId = Guid.NewGuid();

            var requesterNotification = Notification.Create(requesterId, "requester@example.com", "req", NotificationType.Email, enabled: false);
            var assigneeNotification = Notification.Create(assigneeId, "assignee@example.com", "asgn", NotificationType.Email, enabled: true);

            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(requesterNotification);
            _notificationRepository.GetByUserIdAndTypeAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(assigneeNotification);

            var @event = new TaskCommentAdded(
                Guid.NewGuid(),
                "Task Title",
                Guid.NewGuid(),
                "Workspace",
                Guid.NewGuid(),
                "Comment content",
                thirdPartyId,
                "third_party",
                requesterId,
                "requester@example.com",
                "Requester",
                assigneeId,
                "assignee@example.com",
                "Assignee");

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert: only assignee receives it because requester disabled notifications
            await _emailSender.Received(1).SendAsync(
                "assignee@example.com",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_ThrowArgumentNullException_When_EventIsNull()
        {
            // Act
            var act = () => _handler.HandleAsync(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
