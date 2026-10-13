using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Workspaces.Members.AddMember;
using Workspaces.Domain.Workspaces;
using Xunit;

namespace Workspaces.Application.Tests.Workspaces.Members.AddMember
{
    public class AddWorkspaceMemberCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeWorkspaceRepository _repository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly AddWorkspaceMemberCommandHandler _handler;

        public AddWorkspaceMemberCommandHandlerTests()
        {
            _handler = new AddWorkspaceMemberCommandHandler(_uow, _repository, _usersApiClient, _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccessAndCallAddMemberAsync_When_OwnerAddsNewMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            var newMemberId = Guid.NewGuid();
            _usersApiClient.Users[newMemberId] = new UserResult(newMemberId, "member@example.com", "memberuser", "Member FullName");
            _usersApiClient.Users[ownerId] = new UserResult(ownerId, "owner@example.com", "owneruser", "Owner FullName");
            var command = new AddWorkspaceMemberCommand(workspace.Id, newMemberId, ownerId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _repository.AddedMembers.Should().Contain(m => m.UserId == newMemberId && m.WorkspaceId == workspace.Id);
            _uow.SavedChanges.Should().BeTrue();
            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is WorkspaceMemberAdded);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<WorkspaceMemberAdded>().Single();
            publishedEvent.WorkspaceId.Should().Be(workspace.Id);
            publishedEvent.WorkspaceName.Should().Be("Team");
            publishedEvent.MemberUserId.Should().Be(newMemberId);
            publishedEvent.MemberEmail.Should().Be("member@example.com");
            publishedEvent.MemberName.Should().Be("Member FullName");
            publishedEvent.AddedByUserId.Should().Be(ownerId);
            publishedEvent.AddedByName.Should().Be("Owner FullName");
        }

        [Fact]
        public async Task Handle_Should_PublishWorkspaceMemberAddedWithFallbackManagerName_When_ManagerHasNoFullName()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team", ownerId, null);
            _repository.Workspaces.Add(workspace);

            var newMemberId = Guid.NewGuid();
            _usersApiClient.Users[newMemberId] = new UserResult(newMemberId, "member@example.com", "memberuser", null);
            _usersApiClient.Users[ownerId] = new UserResult(ownerId, "owner@example.com", "owneruser", null);
            var command = new AddWorkspaceMemberCommand(workspace.Id, newMemberId, ownerId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<WorkspaceMemberAdded>().Single();
            publishedEvent.MemberName.Should().Be("memberuser");
            publishedEvent.AddedByName.Should().Be("owneruser");
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

            public Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
            {
                var ws = Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                if (ws is null) return Task.FromResult(false);
                return Task.FromResult(ws.OwnerId == userId || ws.Members.Any(m => m.UserId == userId));
            }
        }

        private sealed class FakeUsersApiClient : IUsersApiClient
        {
            public Dictionary<Guid, UserResult> Users { get; } = new();

            public Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            {
                Users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }

            public Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                int page = 1,
                int pageSize = 100,
                string? search = null,
                bool? isWorkspaceMember = null,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyDictionary<Guid, UserResult> result = Users
                    .Where(kvp => userIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return Task.FromResult(result);
            }
        }

        private sealed class FakeEventPublisher : IEventPublisher
        {
            public List<object> PublishedEvents { get; } = new();

            public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
            {
                PublishedEvents.Add(@event);
                return Task.CompletedTask;
            }
        }
    }
}
