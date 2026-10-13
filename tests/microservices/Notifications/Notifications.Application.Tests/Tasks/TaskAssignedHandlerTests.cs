using EmailTemplates;
using FluentAssertions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Application.Tasks;
using Notifications.Domain.Notifications;
using NSubstitute;
using Xunit;

namespace Notifications.Application.Tests.Tasks
{
    public class TaskAssignedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly FrontendOptions _frontendOptions;
        private readonly TaskAssignedHandler _handler;

        public TaskAssignedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _notificationRepository = Substitute.For<INotificationRepository>();
            _frontendOptions = new FrontendOptions
            {
                DashboardPageAddress = "http://localhost:4200/dashboard"
            };

            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_frontendOptions);
            _handler = new TaskAssignedHandler(
                _emailSender,
                _notificationRepository,
                optionsWrapper,
                NullLogger<TaskAssignedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailSuccessfully_When_ValidEventProvidedAndNotificationEnabled()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();
            var assigneeEmail = "assignee@example.com";
            var assigneeName = "Assignee Name";
            var workspaceName = "Engineering Team";
            var assignerName = "Lead Developer";
            var taskTitle = "Implement OAuth Integration";
            var taskDescription = "Connect with Google identity provider.";
            var priority = "Critical";
            var deadline = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

            _notificationRepository.IsEnabledAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(true);

            var @event = new TaskAssigned(
                taskId,
                taskTitle,
                taskDescription,
                workspaceId,
                workspaceName,
                assigneeId,
                assigneeEmail,
                assigneeName,
                assignerId,
                assignerName,
                priority,
                deadline);

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
            capturedSubject.Should().Be(TaskAssignedTemplate.GetSubject(taskTitle, workspaceName));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain(taskTitle);
            capturedBody.Should().Contain(taskDescription);
            capturedBody.Should().Contain(workspaceName);
            capturedBody.Should().Contain(assignerName);
            capturedBody.Should().Contain(assigneeName);
            capturedBody.Should().Contain(priority);
            capturedBody.Should().Contain(taskId.ToString());
            capturedBody.Should().Contain(workspaceId.ToString());
            capturedBody.Should().Contain("View Task");
        }

        [Fact]
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationNotEnabledInDb()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var assigneeEmail = "assignee@example.com";

            _notificationRepository.IsEnabledAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(false);

            var @event = new TaskAssigned(
                taskId,
                "Setup Database",
                null,
                workspaceId,
                "Infrastructure",
                assigneeId,
                assigneeEmail,
                "Assignee Name",
                Guid.NewGuid(),
                "Lead",
                "High",
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
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationDisabledForUser()
        {
            // Arrange
            var assigneeId = Guid.NewGuid();

            _notificationRepository.IsEnabledAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(false);

            var @event = new TaskAssigned(
                Guid.NewGuid(),
                "Design Architecture",
                null,
                Guid.NewGuid(),
                "Design",
                assigneeId,
                "disabled@example.com",
                "User Name",
                Guid.NewGuid(),
                "Manager",
                "Medium",
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
        public async Task HandleAsync_Should_UseFallbackDashboardUrl_When_DashboardAddressNotConfigured()
        {
            // Arrange
            var customOptions = Microsoft.Extensions.Options.Options.Create(new FrontendOptions { DashboardPageAddress = "" });
            var handler = new TaskAssignedHandler(
                _emailSender,
                _notificationRepository,
                customOptions,
                NullLogger<TaskAssignedHandler>.Instance);

            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            _notificationRepository.IsEnabledAsync(assigneeId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(true);

            var @event = new TaskAssigned(
                taskId,
                "Task Title",
                null,
                workspaceId,
                "Design",
                assigneeId,
                "test@example.com",
                "User",
                Guid.NewGuid(),
                "Manager",
                "Low",
                null);

            string? capturedBody = null;
            _emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            capturedBody.Should().Contain($"http://localhost:4200/dashboard?workspaceId={workspaceId}&amp;taskId={taskId}");
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
