using DomainFramework;
using FluentAssertions;
using Workspaces.Application.Workspaces.GetAll;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using Xunit;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tests.Workspaces.GetAll
{
    public class GetAllWorkspacesQueryHandlerTests
    {
        private readonly FakeWorkspaceRepository _repository = new();
        private readonly GetAllWorkspacesQueryHandler _handler;

        public GetAllWorkspacesQueryHandlerTests()
        {
            _handler = new GetAllWorkspacesQueryHandler(_repository);
        }

        [Fact]
        public async Task Handle_Should_ReturnWorkspacesByMembership_When_UserIsNotAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var workspace1 = Workspace.Create("User Workspace", userId, null);
            var workspace2 = Workspace.Create("Other Workspace", otherUserId, null);
            _repository.Workspaces.AddRange(new[] { workspace1, workspace2 });

            var query = new GetAllWorkspacesQuery(userId, false, null, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Id.Should().Be(workspace1.Id);
            result.Value.Items[0].Name.Should().Be("User Workspace");
        }

        [Fact]
        public async Task Handle_Should_ReturnAllWorkspaces_When_UserIsAdmin()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var workspace1 = Workspace.Create("W1", user1, null);
            var workspace2 = Workspace.Create("W2", user2, null);
            _repository.Workspaces.AddRange(new[] { workspace1, workspace2 });

            var query = new GetAllWorkspacesQuery(adminId, true, null, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_Should_FilterByName_When_NameQueryParamProvided()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var workspace1 = Workspace.Create("Alpha Team", adminId, null);
            var workspace2 = Workspace.Create("Beta Team", adminId, null);
            _repository.Workspaces.AddRange(new[] { workspace1, workspace2 });

            var query = new GetAllWorkspacesQuery(adminId, true, "Alpha", 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Alpha Team");
        }

        [Fact]
        public async Task Handle_Should_IncludeTaskCount_When_TaskRepositoryProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspace = Workspace.Create("Alpha Team", userId, null);
            _repository.Workspaces.Add(workspace);

            var fakeTaskRepo = new FakeTaskRepositoryCount(5);
            var handlerWithTasks = new GetAllWorkspacesQueryHandler(_repository, fakeTaskRepo);

            var query = new GetAllWorkspacesQuery(userId, true, null, 1, 10);

            // Act
            var result = await handlerWithTasks.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].TaskCount.Should().Be(5);
        }

        private sealed class FakeTaskRepositoryCount : ITaskRepository
        {
            private readonly int _count;
            public FakeTaskRepositoryCount(int count) => _count = count;

            public Task<int> GetCountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken) =>
                Task.FromResult(_count);

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<TaskItem?>(null);
            public Task<PaginationResult<TaskItem>> GetAllAsync(Guid? workspaceId, string? search, TaskStatus? status, Priority? priority, Guid? assigneeId, Guid? createdById, string? tag, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(PaginationResult<TaskItem>.Create(new List<TaskItem>(), 1, 10, 0));
            public Task<PaginationResult<TaskItem>> GetMyAsync(Guid userId, Guid? workspaceId, string? type, string? search, TaskStatus? status, Priority? priority, string? tag, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(PaginationResult<TaskItem>.Create(new List<TaskItem>(), 1, 10, 0));
            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public List<Workspace> Workspaces { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(Workspaces.FirstOrDefault(w => w.Id == id));

            public Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken)
            {
                var query = Workspaces.Where(w => w.Members.Any(m => m.UserId == userId));
                if (!string.IsNullOrWhiteSpace(name))
                {
                    query = query.Where(w => w.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                }

                var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return Task.FromResult(PaginationResult<Workspace>.Create(items, page, pageSize, query.Count()));
            }

            public Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken)
            {
                var query = Workspaces.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    query = query.Where(w => w.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                }

                var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return Task.FromResult(PaginationResult<Workspace>.Create(items, page, pageSize, query.Count()));
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
    }
}
