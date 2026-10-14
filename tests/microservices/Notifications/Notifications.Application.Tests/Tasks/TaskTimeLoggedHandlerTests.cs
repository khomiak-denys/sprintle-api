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
    public class TaskTimeLoggedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly TaskTimeLoggedHandler _handler;

        public TaskTimeLoggedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _notificationRepository = Substitute.For<INotificationRepository>();
            _handler = new TaskTimeLoggedHandler(
                _emailSender,
                _notificationRepository,
                NullLogger<TaskTimeLoggedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailToRequester_When_TimeLoggedByAssignee()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var requesterEmail = "requester@example.com";
            var timeLogId = Guid.NewGuid();

            var notification = Notification.Create(requesterId, requesterEmail, "requester", NotificationType.Email, enabled: true);
            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskTimeLogged(
                taskId,
                "Optimize Queries",
                workspaceId,
                "Backend Team",
                timeLogId,
                90,
                "Refactored indexes and analyzed query plans.",
                new DateOnly(2026, 10, 14),
                assigneeId,
                "dev_alex",
                requesterId,
                requesterEmail,
                "Sarah Lead");

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
            capturedSubject.Should().Be(TaskTimeLoggedTemplate.GetSubject("Optimize Queries", 90, "Backend Team"));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain("Optimize Queries");
            capturedBody.Should().Contain("Backend Team");
            capturedBody.Should().Contain("dev_alex");
            capturedBody.Should().Contain("1h 30m");
            capturedBody.Should().Contain("Refactored indexes and analyzed query plans.");
        }

        [Fact]
        public async Task HandleAsync_Should_NotSendEmail_When_TimeLoggedByRequesterThemselves()
        {
            // Arrange
            var sameUserId = Guid.NewGuid();
            var @event = new TaskTimeLogged(
                Guid.NewGuid(),
                "Self Task",
                Guid.NewGuid(),
                "Backend Team",
                Guid.NewGuid(),
                60,
                "Self logged time",
                new DateOnly(2026, 10, 14),
                sameUserId,
                "same_user",
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
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationDisabledForRequester()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var notification = Notification.Create(requesterId, "requester@example.com", "requester", NotificationType.Email, enabled: false);
            _notificationRepository.GetByUserIdAndTypeAsync(requesterId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(notification);

            var @event = new TaskTimeLogged(
                Guid.NewGuid(),
                "Task",
                Guid.NewGuid(),
                "Workspace",
                Guid.NewGuid(),
                45,
                null,
                new DateOnly(2026, 10, 14),
                assigneeId,
                "assignee",
                requesterId,
                "requester@example.com",
                "Requester");

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
