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
    public class UserEmailConfirmedHandlerTests
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserEmailConfirmedHandler _handler;

        public UserEmailConfirmedHandlerTests()
        {
            _notificationRepository = Substitute.For<INotificationRepository>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _handler = new UserEmailConfirmedHandler(
                _notificationRepository,
                _unitOfWork,
                NullLogger<UserEmailConfirmedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_EnableNotificationAndUpdateEmail_When_UserEmailConfirmedEventReceived()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var initialEmail = "old@example.com";
            var confirmedEmail = "confirmed@example.com";
            var username = "verifieduser";
            var existingNotification = Notification.Create(userId, initialEmail, username, NotificationType.Email, enabled: false);

            _notificationRepository.GetByUserIdAndTypeAsync(
                userId,
                NotificationType.Email,
                Arg.Any<CancellationToken>())
                .Returns(existingNotification);

            var @event = new UserEmailConfirmed(userId, confirmedEmail);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            existingNotification.Enabled.Should().BeTrue();
            existingNotification.Email.Should().Be(confirmedEmail);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_Should_DoNothing_When_NotificationNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var @event = new UserEmailConfirmed(userId, "test@example.com");

            _notificationRepository.GetByUserIdAndTypeAsync(
                userId,
                NotificationType.Email,
                Arg.Any<CancellationToken>())
                .Returns((Notification?)null);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
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
