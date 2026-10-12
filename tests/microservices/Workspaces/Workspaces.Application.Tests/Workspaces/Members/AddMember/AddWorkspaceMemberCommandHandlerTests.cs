using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Workspaces.Members.AddMember;
using Workspaces.Domain.Workspaces;
using Xunit;

namespace Workspaces.Application.Tests.Workspaces.Members.AddMember
{
    public class AddWorkspaceMemberCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeWorkspaceRepository _repository = new();
        private readonly AddWorkspaceMemberCommandHandler _handler;

        public AddWorkspaceMemberCommandHandlerTests()
        {
            _handler = new AddWorkspaceMemberCommandHandler(_uow, _repository);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccessAndCallAddMemberAsync_When_OwnerAddsNewMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            var newMemberId = Guid.NewGuid();
            var command = new AddWorkspaceMemberCommand(workspace.Id, newMemberId, ownerId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _repository.AddedMembers.Should().Contain(m => m.UserId == newMemberId && m.WorkspaceId == workspace.Id);
            _uow.SavedChanges.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_AdminAddsNewMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var adminUserId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            var newMemberId = Guid.NewGuid();
            var command = new AddWorkspaceMemberCommand(workspace.Id, newMemberId, adminUserId, true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _repository.AddedMembers.Should().Contain(m => m.UserId == newMemberId);
            _uow.SavedChanges.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFoundError_When_WorkspaceDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var command = new AddWorkspaceMemberCommand(nonExistentId, Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SavedChanges.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbiddenError_When_NotOwnerAndNotAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            var command = new AddWorkspaceMemberCommand(workspace.Id, Guid.NewGuid(), otherUserId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            _uow.SavedChanges.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnAlreadyExistsError_When_UserIsAlreadyMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            // Owner is already added on creation
            var command = new AddWorkspaceMemberCommand(workspace.Id, ownerId, ownerId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<AlreadyExistsError>();
            _repository.AddedMembers.Should().BeEmpty();
            _uow.SavedChanges.Should().BeFalse();
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public bool SavedChanges { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
            {
                SavedChanges = true;
                return Task.FromResult(1);
            }
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public List<Workspace> Workspaces { get; } = new();
            public List<WorkspaceMember> AddedMembers { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(Workspaces.FirstOrDefault(w => w.Id == id));

            public Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(Array.Empty<Workspace>(), page, pageSize, 0));

            public Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(Array.Empty<Workspace>(), page, pageSize, 0));

            public Task AddAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                Workspaces.Add(workspace);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(Workspace workspace, CancellationToken cancellationToken)
            {
                Workspaces.Remove(workspace);
                return Task.CompletedTask;
            }

            public Task AddMemberAsync(WorkspaceMember member, CancellationToken cancellationToken)
            {
                AddedMembers.Add(member);
                return Task.CompletedTask;
            }
        }
    }
}
