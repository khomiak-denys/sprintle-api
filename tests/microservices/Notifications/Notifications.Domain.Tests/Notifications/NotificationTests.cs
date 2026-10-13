using FluentAssertions;
using Notifications.Domain.Notifications;
using Xunit;

namespace Notifications.Domain.Tests.Notifications
{
    public class NotificationTests
    {
        [Fact]
        public void Create_Should_InitializeNotificationWithDefaults_When_ValidParametersProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "john.doe@example.com";
            var username = "johndoe";

            // Act
            var notification = Notification.Create(userId, email, username);

            // Assert
            notification.Should().NotBeNull();
            notification.Id.Should().NotBeEmpty();
            notification.UserId.Should().Be(userId);
            notification.Email.Should().Be(email);
            notification.Username.Should().Be(username);
            notification.NotificationType.Should().Be(NotificationType.Email);
            notification.Enabled.Should().BeTrue();
            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            notification.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_Should_InitializeWithCustomId_When_ExplicitIdProvided()
        {
            // Arrange
            var customId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var email = "jane.doe@example.com";
            var username = "janedoe";

            // Act
            var notification = Notification.Create(customId, userId, email, username, NotificationType.Email, false);

            // Assert
            notification.Id.Should().Be(customId);
            notification.UserId.Should().Be(userId);
            notification.Email.Should().Be(email);
            notification.Username.Should().Be(username);
            notification.NotificationType.Should().Be(NotificationType.Email);
            notification.Enabled.Should().BeFalse();
            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            notification.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void UpdateEmail_Should_ModifyEmailAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "old@example.com", "user1");
            var newEmail = "new@example.com";

            // Act
            notification.UpdateEmail(newEmail);

            // Assert
            notification.Email.Should().Be(newEmail);
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void UpdateUsername_Should_ModifyUsernameAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "user@example.com", "oldusername");
            var newUsername = "newusername";

            // Act
            notification.UpdateUsername(newUsername);

            // Assert
            notification.Username.Should().Be(newUsername);
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void SetEnabled_Should_UpdateEnabledStatusAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "user@example.com", "user1", NotificationType.Email, true);

            // Act
            notification.SetEnabled(false);

            // Assert
            notification.Enabled.Should().BeFalse();
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Enable_Should_SetEnabledToTrueAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "user@example.com", "user1", NotificationType.Email, false);

            // Act
            notification.Enable();

            // Assert
            notification.Enabled.Should().BeTrue();
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Disable_Should_SetEnabledToFalseAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "user@example.com", "user1", NotificationType.Email, true);

            // Act
            notification.Disable();

            // Assert
            notification.Enabled.Should().BeFalse();
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Update_Should_ModifyDetailsAndSetUpdatedAt_When_Called()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "old@example.com", "olduser", NotificationType.Email, false);

            // Act
            notification.Update("updated@example.com", "updateduser", true);

            // Assert
            notification.Email.Should().Be("updated@example.com");
            notification.Username.Should().Be("updateduser");
            notification.Enabled.Should().BeTrue();
            notification.UpdatedAt.Should().NotBeNull();
            notification.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}
