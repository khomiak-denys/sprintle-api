using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Notifications.Infrastructure.Tests.Repositories
{
    public class NotificationRepositoryTests : IDisposable
    {
        private readonly NotificationsDbContext _context;
        private readonly NotificationRepository _repository;

        public NotificationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NotificationsDbContext(options);
            _repository = new NotificationRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task AddAsync_Should_PersistNotification_When_Saved()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "test@example.com", "testuser");

            // Act
            await _repository.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Assert
            var result = await _context.Notifications.FindAsync(notification.Id);
            result.Should().NotBeNull();
            result!.Email.Should().Be("test@example.com");
            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnNotification_When_NotificationExists()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "find@example.com", "finduser");
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(notification.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(notification.Id);
            result.Email.Should().Be("find@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnNull_When_NotificationDoesNotExist()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_Should_ReturnNotification_When_UserHasNotification()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notification = Notification.Create(userId, "user@example.com", "user");
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task GetByUserIdAndTypeAsync_Should_ReturnNotification_When_MatchingUserAndType()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notification = Notification.Create(userId, "channel@example.com", "channeluser", NotificationType.Email);
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAndTypeAsync(userId, NotificationType.Email);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.NotificationType.Should().Be(NotificationType.Email);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_Should_ReturnAllNotificationsForUser_When_Called()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var n1 = Notification.Create(targetUserId, "target1@example.com", "user1", NotificationType.Email);
            var n2 = Notification.Create(otherUserId, "other@example.com", "user2", NotificationType.Email);

            await _context.Notifications.AddRangeAsync(n1, n2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllByUserIdAsync(targetUserId);

            // Assert
            result.Should().HaveCount(1);
            result[0].UserId.Should().Be(targetUserId);
        }

        [Fact]
        public async Task GetAllAsync_Should_ReturnAllNotifications_When_Called()
        {
            // Arrange
            var n1 = Notification.Create(Guid.NewGuid(), "all1@example.com", "all1");
            var n2 = Notification.Create(Guid.NewGuid(), "all2@example.com", "all2");

            await _context.Notifications.AddRangeAsync(n1, n2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task Update_Should_ModifyExistingNotification_When_Saved()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "before@example.com", "beforeuser");
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Act
            notification.Update("after@example.com", "afteruser", false);
            _repository.Update(notification);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _context.Notifications.FindAsync(notification.Id);
            updated.Should().NotBeNull();
            updated!.Email.Should().Be("after@example.com");
            updated.Username.Should().Be("afteruser");
            updated.Enabled.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_Should_RemoveNotification_When_Saved()
        {
            // Arrange
            var notification = Notification.Create(Guid.NewGuid(), "delete@example.com", "deleteuser");
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Act
            _repository.Delete(notification);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _context.Notifications.FindAsync(notification.Id);
            deleted.Should().BeNull();
        }
    }
}
