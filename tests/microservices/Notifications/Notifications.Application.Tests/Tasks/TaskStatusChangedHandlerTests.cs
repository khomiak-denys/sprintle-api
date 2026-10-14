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
    public class TaskStatusChangedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly TaskStatusChangedHandler _handler;

        public TaskStatusChangedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _notificationRepository = Substitute.For<INotificationRepository>();
            _handler = new TaskStatusChangedHandler(
                _emailSender,
                _notificationRepository,
                NullLogger<TaskStatusChangedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailToRequester_When_TaskSubmittedForReview()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";
            var requesterName = "Requester Name";
            var assigneeUsername = "developer_alex";

            var notification = Notification.Create(requesterId, requesterEmail, "requester_user", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskStatusChanged(
                taskId,
                "API Optimization",
                workspaceId,
                "Backend Workspace",
                "InProgress",
                "InReview",
                assigneeId,
                assigneeUsername,
                requesterId,
                requesterEmail,
                requesterName,
                assigneeId,
                "alex@example.com",
                assigneeUsername);

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
            capturedSubject.Should().Be(TaskStatusChangedTemplate.GetSubject("API Optimization", "InReview", "Backend Workspace"));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain("InProgress");
            capturedBody.Should().Contain("InReview");
            capturedBody.Should().Contain(assigneeUsername);
            capturedBody.Should().Contain("API Optimization");
            capturedBody.Should().Contain("Backend Workspace");
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailToAssignee_When_TaskApproved()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var assigneeEmail = "assignee@example.com";
            var assigneeName = "Assignee Developer";
            var managerUsername = "manager_sarah";

            var notification = Notification.Create(assigneeId, assigneeEmail, "assignee_user", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskStatusChanged(
                taskId,
                "Payment Webhooks",
                workspaceId,
                "Fintech Platform",
                "InReview",
                "Done",
                requesterId,
                managerUsername,
                requesterId,
                "sarah@example.com",
                managerUsername,
                assigneeId,
                assigneeEmail,
                assigneeName);

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
            capturedSubject.Should().Be(TaskStatusChangedTemplate.GetSubject("Payment Webhooks", "Done", "Fintech Platform"));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain("InReview");
            capturedBody.Should().Contain("Done");
            capturedBody.Should().Contain(managerUsername);
            capturedBody.Should().Contain("Payment Webhooks");
            capturedBody.Should().Contain("Task Approved");
        }

        [Fact]
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationDisabledForUser()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var notification = Notification.Create(requesterId, "disabled@example.com", "user", NotificationType.Email, enabled: false);

            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskStatusChanged(
                Guid.NewGuid(),
                "Title",
                Guid.NewGuid(),
                "Workspace",
                "InProgress",
                "InReview",
                Guid.NewGuid(),
                "changer",
                requesterId,
                "disabled@example.com",
                "Requester",
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
        public async Task HandleAsync_Should_NotSendEmail_When_UserChangedOwnStatus()
        {
            // Arrange
            var sameUserId = Guid.NewGuid();

            var @event = new TaskStatusChanged(
                Guid.NewGuid(),
                "Self change",
                Guid.NewGuid(),
                "Workspace",
                "InProgress",
                "InReview",
                sameUserId,
                "same_user",
                sameUserId,
                "self@example.com",
                "Same User",
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
        public async Task HandleAsync_Should_SendEmailToAssignee_When_TaskCancelled()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var assigneeEmail = "assignee@example.com";
            var assigneeName = "Assignee Developer";
            var managerUsername = "manager_sarah";

            var notification = Notification.Create(assigneeId, assigneeEmail, "assignee_user", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskStatusChanged(
                taskId,
                "Refactor Module",
                workspaceId,
                "Platform Team",
                "InProgress",
                "Cancelled",
                creatorId,
                managerUsername,
                creatorId,
                "sarah@example.com",
                managerUsername,
                assigneeId,
                assigneeEmail,
                assigneeName);

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
            capturedSubject.Should().Be(TaskStatusChangedTemplate.GetSubject("Refactor Module", "Cancelled", "Platform Team"));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain("Refactor Module");
            capturedBody.Should().Contain("Task Cancelled");
            capturedBody.Should().Contain("InProgress");
            capturedBody.Should().Contain("Cancelled");
            capturedBody.Should().Contain(managerUsername);
        }

        [Fact]
        public async Task HandleAsync_Should_NotSendEmail_When_TaskCancelledAndUnassigned()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();

            var @event = new TaskStatusChanged(
                taskId,
                "Unassigned Task",
                workspaceId,
                "Platform Team",
                "Todo",
                "Cancelled",
                creatorId,
                "creator_user",
                creatorId,
                "creator@example.com",
                "Creator User",
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
        public async Task HandleAsync_Should_NotSendEmail_When_TaskCancelledByAssigneeThemselves()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();

            var @event = new TaskStatusChanged(
                taskId,
                "Self Cancelled Task",
                workspaceId,
                "Platform Team",
                "InProgress",
                "Cancelled",
                assigneeId,
                "assignee_user",
                creatorId,
                "creator@example.com",
                "Creator User",
                assigneeId,
                "assignee@example.com",
                "Assignee User");

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
        public async Task HandleAsync_Should_ThrowArgumentNullException_When_EventIsNull()
        {
            // Act
            var act = () => _handler.HandleAsync(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
