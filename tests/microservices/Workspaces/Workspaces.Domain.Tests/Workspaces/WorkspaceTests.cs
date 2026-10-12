using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Domain.Workspaces;
using Xunit;

namespace Workspaces.Domain.Tests.Workspaces
{
    public class WorkspaceTests
    {
        [Fact]
        public void Create_Should_InitializeWorkspaceWithMembers_When_ValidInput()
        {
            // Arrange
            var name = "Engineering";
            var ownerId = Guid.NewGuid();
            var description = "Engineering workspace";

            // Act
            var workspace = Workspace.Create(name, ownerId, description);

            // Assert
            workspace.Should().NotBeNull();
            workspace.Id.Should().NotBeEmpty();
            workspace.Name.Should().Be(name);
            workspace.OwnerId.Should().Be(ownerId);
            workspace.Description.Should().Be(description);
            workspace.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            workspace.UpdatedAt.Should().BeNull();
            workspace.Members.Should().HaveCount(1);
            workspace.Members.First().UserId.Should().Be(ownerId);
            workspace.Members.First().WorkspaceId.Should().Be(workspace.Id);
        }

        [Fact]
        public void Update_Should_UpdatePropertiesAndModifyTimestamp_When_Called()
        {
            // Arrange
            var workspace = Workspace.Create("Old Name", Guid.NewGuid(), "Old Description");

            // Act
            workspace.Update("New Name", "New Description");

            // Assert
            workspace.Name.Should().Be("New Name");
            workspace.Description.Should().Be("New Description");
            workspace.UpdatedAt.Should().NotBeNull();
            workspace.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void AddMember_Should_AddMember_When_UserIsNotMember()
        {
            // Arrange
            var workspace = Workspace.Create("Team", Guid.NewGuid(), null);
            var newUserId = Guid.NewGuid();

            // Act
            var result = workspace.AddMember(newUserId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.UserId.Should().Be(newUserId);
            workspace.Members.Should().HaveCount(2);
            workspace.Members.Should().Contain(m => m.UserId == newUserId);
        }

        [Fact]
        public void AddMember_Should_ReturnAlreadyExistsError_When_UserIsAlreadyMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);

            // Act
            var result = workspace.AddMember(ownerId);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<AlreadyExistsError>();
            workspace.Members.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveMember_Should_RemoveMember_When_UserIsMember()
        {
            // Arrange
            var workspace = Workspace.Create("Team", Guid.NewGuid(), null);
            var memberId = Guid.NewGuid();
            workspace.AddMember(memberId);

            // Act
            var result = workspace.RemoveMember(memberId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            workspace.Members.Should().HaveCount(1);
            workspace.Members.Should().NotContain(m => m.UserId == memberId);
        }

        [Fact]
        public void RemoveMember_Should_ReturnInvalidArgumentError_When_AttemptingToRemoveOwner()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);

            // Act
            var result = workspace.RemoveMember(ownerId);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
            workspace.Members.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveMember_Should_ReturnNotFoundError_When_UserIsNotMember()
        {
            // Arrange
            var workspace = Workspace.Create("Team", Guid.NewGuid(), null);
            var nonMemberId = Guid.NewGuid();

            // Act
            var result = workspace.RemoveMember(nonMemberId);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }
    }
}
