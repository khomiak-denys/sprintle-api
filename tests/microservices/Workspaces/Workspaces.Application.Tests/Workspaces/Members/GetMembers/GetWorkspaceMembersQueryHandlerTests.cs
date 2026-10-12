using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Workspaces.Members.GetMembers;
using Workspaces.Domain.Workspaces;
using Xunit;

namespace Workspaces.Application.Tests.Workspaces.Members.GetMembers
{
    public class GetWorkspaceMembersQueryHandlerTests
    {
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly GetWorkspaceMembersQueryHandler _handler;

        public GetWorkspaceMembersQueryHandlerTests()
        {
            _handler = new GetWorkspaceMembersQueryHandler(_workspaceRepository, _usersApiClient, NullLogger<GetWorkspaceMembersQueryHandler>.Instance);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_WorkspaceDoesNotExist()
        {
            // Arrange
            var query = new GetWorkspaceMembersQuery(Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotMemberOrAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Confidential", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var strangerId = Guid.NewGuid();
            var query = new GetWorkspaceMembersQuery(workspace.Id, strangerId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnMembersWithUserDetails_When_UserIsMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Space", ownerId, "Desc");
            workspace.AddMember(memberId);
            _workspaceRepository.Workspaces.Add(workspace);

            var ownerUser = new UserResult(ownerId, "owner@example.com", "owner_usr", "Workspace Owner");
            var memberUser = new UserResult(memberId, "member@example.com", "member_usr", "Regular Member");
            _usersApiClient.Users[ownerId] = ownerUser;
            _usersApiClient.Users[memberId] = memberUser;

            var query = new GetWorkspaceMembersQuery(workspace.Id, memberId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(2);
            result.Value.Page.Should().Be(1);
            result.Value.PageSize.Should().Be(3);
            result.Value.Items.Should().HaveCount(2);

            var ownerResult = result.Value.Items.Single(m => m.Id == ownerId);
            ownerResult.Email.Should().Be("owner@example.com");
            ownerResult.UserName.Should().Be("owner_usr");
            ownerResult.FullName.Should().Be("Workspace Owner");
            ownerResult.Id.Should().Be(ownerId);

            var regularResult = result.Value.Items.Single(m => m.Id == memberId);
            regularResult.Email.Should().Be("member@example.com");
            regularResult.UserName.Should().Be("member_usr");
            regularResult.FullName.Should().Be("Regular Member");
            regularResult.Id.Should().Be(memberId);

            _usersApiClient.BatchCallCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_ReturnMembersWithUserDetails_When_UserIsAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Space", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var adminId = Guid.NewGuid();
            var ownerUser = new UserResult(ownerId, "owner@example.com", "owner_usr", "Workspace Owner");
            _usersApiClient.Users[ownerId] = ownerUser;

            var query = new GetWorkspaceMembersQuery(workspace.Id, adminId, true);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Id.Should().Be(ownerId);
        }

        [Fact]
        public async Task Handle_Should_FilterBySearchContains_When_SearchProvided()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var member1Id = Guid.NewGuid();
            var member2Id = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Space", ownerId, "Desc");
            workspace.AddMember(member1Id);
            workspace.AddMember(member2Id);
            _workspaceRepository.Workspaces.Add(workspace);

            _usersApiClient.Users[ownerId] = new UserResult(ownerId, "alice@alpha.com", "alice_w", "Alice Wonderland");
            _usersApiClient.Users[member1Id] = new UserResult(member1Id, "bob@beta.org", "bob_builder", "Bob Builder");
            _usersApiClient.Users[member2Id] = new UserResult(member2Id, "charlie@gamma.net", "charlie_c", "Charlie Chaplin");

            // Search by username contains "builder"
            var queryUser = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false, Search: "builder");
            var resultUser = await _handler.Handle(queryUser, CancellationToken.None);
            resultUser.IsSuccess.Should().BeTrue();
            resultUser.Value.TotalCount.Should().Be(1);
            resultUser.Value.Items.Should().ContainSingle(m => m.Id == member1Id);

            // Search by full name contains "wonder" (case-insensitive)
            var queryName = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false, Search: "WONDER");
            var resultName = await _handler.Handle(queryName, CancellationToken.None);
            resultName.IsSuccess.Should().BeTrue();
            resultName.Value.TotalCount.Should().Be(1);
            resultName.Value.Items.Should().ContainSingle(m => m.Id == ownerId);

            // Search by email contains "gamma.net"
            var queryEmail = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false, Search: "gamma");
            var resultEmail = await _handler.Handle(queryEmail, CancellationToken.None);
            resultEmail.IsSuccess.Should().BeTrue();
            resultEmail.Value.TotalCount.Should().Be(1);
            resultEmail.Value.Items.Should().ContainSingle(m => m.Id == member2Id);
        }

        [Fact]
        public async Task Handle_Should_PaginateResults_WithDefaultPageSizeThree_When_Invoked()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Space", ownerId, "Desc");
            var memberIds = new List<Guid>();
            for (int i = 1; i <= 5; i++)
            {
                var mid = Guid.NewGuid();
                memberIds.Add(mid);
                workspace.AddMember(mid);
                _usersApiClient.Users[mid] = new UserResult(mid, $"user{i}@test.com", $"user_{i}", $"User {i}");
            }
            _usersApiClient.Users[ownerId] = new UserResult(ownerId, "owner@test.com", "owner", "Owner");
            _workspaceRepository.Workspaces.Add(workspace);

            // Total 6 members (owner + 5 members)
            // Page 1 with default pageSize 3
            var queryPage1 = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false, Page: 1, PageSize: 3);
            var result1 = await _handler.Handle(queryPage1, CancellationToken.None);

            result1.IsSuccess.Should().BeTrue();
            result1.Value.TotalCount.Should().Be(6);
            result1.Value.Page.Should().Be(1);
            result1.Value.PageSize.Should().Be(3);
            result1.Value.Items.Should().HaveCount(3);

            // Page 2
            var queryPage2 = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false, Page: 2, PageSize: 3);
            var result2 = await _handler.Handle(queryPage2, CancellationToken.None);

            result2.IsSuccess.Should().BeTrue();
            result2.Value.TotalCount.Should().Be(6);
            result2.Value.Page.Should().Be(2);
            result2.Value.PageSize.Should().Be(3);
            result2.Value.Items.Should().HaveCount(3);
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public List<Workspace> Workspaces { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            {
                var ws = Workspaces.FirstOrDefault(w => w.Id == id);
                return Task.FromResult(ws);
            }

            public Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken)
            {
                return Task.FromResult(PaginationResult<Workspace>.Create(new List<Workspace>(), page, pageSize, 0));
            }

            public Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken)
            {
                return Task.FromResult(PaginationResult<Workspace>.Create(new List<Workspace>(), page, pageSize, 0));
            }

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

        [Fact]
        public async Task Handle_Should_PassIsWorkspaceMemberTrue_When_CallingUsersApiClient()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var workspace = Workspace.Create("Team Space", ownerId, "Desc");
            _workspaceRepository.Workspaces.Add(workspace);

            var query = new GetWorkspaceMembersQuery(workspace.Id, ownerId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _usersApiClient.LastIsWorkspaceMember.Should().BeTrue();
        }

        private sealed class FakeUsersApiClient : IUsersApiClient
        {
            public Dictionary<Guid, UserResult> Users { get; } = new();
            public int BatchCallCount { get; private set; }
            public bool? LastIsWorkspaceMember { get; private set; }

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
                BatchCallCount++;
                LastIsWorkspaceMember = isWorkspaceMember;
                var query = Users.Where(kvp => userIds.Contains(kvp.Key));
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var trimmed = search.Trim();
                    query = query.Where(kvp =>
                        (!string.IsNullOrEmpty(kvp.Value.UserName) && kvp.Value.UserName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(kvp.Value.FullName) && kvp.Value.FullName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(kvp.Value.Email) && kvp.Value.Email.Contains(trimmed, StringComparison.OrdinalIgnoreCase)));
                }

                IReadOnlyDictionary<Guid, UserResult> result = query
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return Task.FromResult(result);
            }
        }
    }
}
