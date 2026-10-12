using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Workspaces.Members.GetPotentialMembers;
using Workspaces.Domain.Workspaces;
using Xunit;

namespace Workspaces.Application.Tests.Workspaces.Members.GetPotentialMembers
{
    public class GetPotentialWorkspaceMembersQueryHandlerTests
    {
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly GetPotentialWorkspaceMembersQueryHandler _handler;

        public GetPotentialWorkspaceMembersQueryHandlerTests()
        {
            _handler = new GetPotentialWorkspaceMembersQueryHandler(_workspaceRepository, _usersApiClient, NullLogger<GetPotentialWorkspaceMembersQueryHandler>.Instance);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_WorkspaceDoesNotExist()
        {
            // Arrange
            var query = new GetPotentialWorkspaceMembersQuery(Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_CallerIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Engineering", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var strangerId = Guid.NewGuid();
            var query = new GetPotentialWorkspaceMembersQuery(workspace.Id, strangerId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnPotentialMembers_When_CallerIsOwner()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Engineering", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var potentialUser = new UserResult(
                Guid.NewGuid(), "dev@example.com", "devuser", "Developer");
            _usersApiClient.Users[potentialUser.Id] = potentialUser;

            var query = new GetPotentialWorkspaceMembersQuery(workspace.Id, ownerId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle(u => u.Id == potentialUser.Id);
            _usersApiClient.LastIsWorkspaceMember.Should().Be(false);
        }

        [Fact]
        public async Task Handle_Should_ReturnPotentialMembers_When_CallerIsAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Engineering", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var adminUserId = Guid.NewGuid();
            var potentialUser = new UserResult(
                Guid.NewGuid(), "admin.invite@example.com", "admininvite", "Admin Invitee");
            _usersApiClient.Users[potentialUser.Id] = potentialUser;

            var query = new GetPotentialWorkspaceMembersQuery(workspace.Id, adminUserId, true);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle(u => u.Id == potentialUser.Id);
            _usersApiClient.LastIsWorkspaceMember.Should().Be(false);
        }

        [Fact]
        public async Task Handle_Should_PassCurrentMemberIdsToExclude_When_QueryingUsersApi()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Engineering", ownerId, "Desc");
            var member2Id = Guid.NewGuid();
            workspace.AddMember(member2Id);
            _workspaceRepository.Workspaces.Add(workspace);

            var query = new GetPotentialWorkspaceMembersQuery(workspace.Id, ownerId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _usersApiClient.LastExcludedUserIds.Should().NotBeNull();
            _usersApiClient.LastExcludedUserIds.Should().Contain(ownerId);
            _usersApiClient.LastExcludedUserIds.Should().Contain(member2Id);
            _usersApiClient.LastIsWorkspaceMember.Should().Be(false);
        }

        [Fact]
        public async Task Handle_Should_PassSearchAndPagination_When_Provided()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Engineering", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var query = new GetPotentialWorkspaceMembersQuery(
                workspace.Id, ownerId, false, Search: "john", Page: 2, PageSize: 5);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _usersApiClient.LastSearch.Should().Be("john");
            _usersApiClient.LastPage.Should().Be(2);
            _usersApiClient.LastPageSize.Should().Be(5);
            _usersApiClient.LastIsWorkspaceMember.Should().Be(false);
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public List<Workspace> Workspaces { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(Workspaces.FirstOrDefault(w => w.Id == id));

            public Task<PaginationResult<Workspace>> GetByMemberAsync(
                Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(new List<Workspace>(), page, pageSize, 0));

            public Task<PaginationResult<Workspace>> GetAllAsync(
                string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(new List<Workspace>(), page, pageSize, 0));

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
        }

        private sealed class FakeUsersApiClient : IUsersApiClient
        {
            public Dictionary<Guid, UserResult> Users { get; } = new();
            public IReadOnlyCollection<Guid>? LastExcludedUserIds { get; private set; }
            public int? LastPage { get; private set; }
            public int? LastPageSize { get; private set; }
            public string? LastSearch { get; private set; }
            public bool? LastIsWorkspaceMember { get; private set; }

            public Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                int page = 1,
                int pageSize = 100,
                string? search = null,
                bool? isWorkspaceMember = null,
                CancellationToken cancellationToken = default)
            {
                LastExcludedUserIds = userIds;
                LastPage = page;
                LastPageSize = pageSize;
                LastSearch = search;
                LastIsWorkspaceMember = isWorkspaceMember;

                IReadOnlyDictionary<Guid, UserResult> result = Users;
                return Task.FromResult(result);
            }
        }
    }
}
