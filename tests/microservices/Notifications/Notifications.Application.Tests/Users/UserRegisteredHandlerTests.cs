using FluentAssertions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Application.Abstractions;
using Notifications.Application.Users;
using Notifications.Domain.Notifications;
using NSubstitute;
using Xunit;

namespace Notifications.Application.Tests.Users
{
    public class UserRegisteredHandlerTests
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserRegisteredHandler _handler;

        public UserRegisteredHandlerTests()
        {
            _notificationRepository = Substitute.For<INotificationRepository>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _handler = new UserRegisteredHandler(
                _notificationRepository,
                _unitOfWork,
                NullLogger<UserRegisteredHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_CreateDisabledNotification_When_UserRegisteredEventReceived()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "newuser@example.com";
            var username = "newuser";
            var @event = new UserRegistered(userId, username, email);

            _notificationRepository.GetByUserIdAndTypeAsync(
                userId,
                NotificationType.Email,
                Arg.Any<CancellationToken>())
                .Returns((Notification?)null);

            Notification? capturedNotification = null;
            await _notificationRepository.AddAsync(
                Arg.Do<Notification>(n => capturedNotification = n),
                Arg.Any<CancellationToken>());

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            capturedNotification.Should().NotBeNull();
            capturedNotification!.UserId.Should().Be(userId);
            capturedNotification.Email.Should().Be(email);
            capturedNotification.Username.Should().Be(username);
            capturedNotification.NotificationType.Should().Be(NotificationType.Email);
            capturedNotification.Enabled.Should().BeFalse();

            await _notificationRepository.Received(1).AddAsync(
                Arg.Any<Notification>(),
                Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_NotCreateNotification_When_NotificationAlreadyExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "existing@example.com";
            var username = "existing";
            var @event = new UserRegistered(userId, username, email);

            var existing = Notification.Create(userId, email, username, NotificationType.Email, enabled: false);
            _notificationRepository.GetByUserIdAndTypeAsync(
                userId,
                NotificationType.Email,
                Arg.Any<CancellationToken>())
                .Returns(existing);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _notificationRepository.DidNotReceive().AddAsync(
                Arg.Any<Notification>(),
                Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_ThrowArgumentNullException_When_EventIsNull()
        {
            // Act
            var act = () => _handler.HandleAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
