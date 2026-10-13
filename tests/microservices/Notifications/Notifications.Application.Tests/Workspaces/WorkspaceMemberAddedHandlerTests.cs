using EmailTemplates;
using FluentAssertions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Application.Workspaces;
using Notifications.Domain.Notifications;
using NSubstitute;
using Xunit;

namespace Notifications.Application.Tests.Workspaces
{
    public class WorkspaceMemberAddedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepository;
        private readonly FrontendOptions _frontendOptions;
        private readonly WorkspaceMemberAddedHandler _handler;

        public WorkspaceMemberAddedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _notificationRepository = Substitute.For<INotificationRepository>();
            _frontendOptions = new FrontendOptions
            {
                DashboardPageAddress = "http://localhost:4200/dashboard"
            };

            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_frontendOptions);
            _handler = new WorkspaceMemberAddedHandler(
                _emailSender,
                _notificationRepository,
                optionsWrapper,
                NullLogger<WorkspaceMemberAddedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailSuccessfully_When_ValidEventProvidedAndNotificationEnabled()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var managerId = Guid.NewGuid();
            var memberEmail = "member@example.com";
            var memberName = "Member Name";
            var workspaceName = "Engineering Team";
            var managerName = "Manager Name";

            _notificationRepository.IsEnabledAsync(memberId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(true);

            var @event = new WorkspaceMemberAdded(
                workspaceId,
                workspaceName,
                memberId,
                memberEmail,
                memberName,
                managerId,
                managerName);

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

            capturedTo.Should().Be(memberEmail);
            capturedSubject.Should().Be(WorkspaceMemberAddedTemplate.GetSubject(workspaceName));
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain(workspaceName);
            capturedBody.Should().Contain(managerName);
            capturedBody.Should().Contain(memberName);
            capturedBody.Should().Contain(workspaceId.ToString());
            capturedBody.Should().Contain("Go to Workspace");
        }

        [Fact]
        public async Task HandleAsync_Should_SkipSendingEmail_When_NotificationNotEnabledInDb()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var memberEmail = "member@example.com";

            _notificationRepository.IsEnabledAsync(memberId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(false);

            var @event = new WorkspaceMemberAdded(
                workspaceId,
                "Marketing",
                memberId,
                memberEmail,
                "Member Name",
                Guid.NewGuid(),
                "Manager Name");

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
            var memberId = Guid.NewGuid();

            _notificationRepository.IsEnabledAsync(memberId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(false);

            var @event = new WorkspaceMemberAdded(
                Guid.NewGuid(),
                "Design",
                memberId,
                "disabled@example.com",
                "User Name",
                Guid.NewGuid(),
                "Manager");

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
            var handler = new WorkspaceMemberAddedHandler(
                _emailSender,
                _notificationRepository,
                customOptions,
                NullLogger<WorkspaceMemberAddedHandler>.Instance);

            var workspaceId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            _notificationRepository.IsEnabledAsync(memberId, NotificationType.Email, Arg.Any<CancellationToken>())
                .Returns(true);

            var @event = new WorkspaceMemberAdded(
                workspaceId,
                "Design",
                memberId,
                "test@example.com",
                "User",
                Guid.NewGuid(),
                "Manager");

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
            capturedBody.Should().Contain($"http://localhost:4200/dashboard?workspaceId={workspaceId}");
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
