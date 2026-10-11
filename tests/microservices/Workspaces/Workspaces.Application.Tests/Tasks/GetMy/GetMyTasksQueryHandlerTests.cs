using DomainFramework;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetMy;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetMy
{
    public class GetMyTasksQueryHandlerTests
    {
        private readonly FakeTaskRepository _repository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly GetMyTasksQueryHandler _handler;

        public GetMyTasksQueryHandlerTests()
        {
            _handler = new GetMyTasksQueryHandler(_repository, _usersApiClient);
        }

        [Fact]
        public async Task Handle_Should_ReturnTasks_When_QueryHasDefaults()
        {
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var task = TaskItem.Create(workspaceId, "My Task", "Desc", Priority.Medium, null, userId, userId);
            _repository.TasksToReturn.Add(task);

            var query = new GetMyTasksQuery(userId);
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Title.Should().Be("My Task");
            _repository.LastUserId.Should().Be(userId);
            _repository.LastType.Should().BeNull();
            _repository.LastSearch.Should().BeNull();
            _repository.LastStatus.Should().BeNull();
            _repository.LastPriority.Should().BeNull();
            _repository.LastTag.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_PassAllFiltersToRepository_When_Specified()
        {
            var userId = Guid.NewGuid();
            var query = new GetMyTasksQuery(
                UserId: userId,
                Type: "assigned",
                Search: "deploy",
                Status: "InReview",
                Priority: "Critical",
                Tag: "ops",
                Page: 3,
                PageSize: 15);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastUserId.Should().Be(userId);
            _repository.LastType.Should().Be("assigned");
            _repository.LastSearch.Should().Be("deploy");
            _repository.LastStatus.Should().Be(TaskStatus.InReview);
            _repository.LastPriority.Should().Be(Priority.Critical);
            _repository.LastTag.Should().Be("ops");
            _repository.LastPage.Should().Be(3);
            _repository.LastPageSize.Should().Be(15);
        }

        [Fact]
        public async Task Handle_Should_ParseAllAsNull_For_StatusAndPriority()
        {
            var userId = Guid.NewGuid();
            var query = new GetMyTasksQuery(
                UserId: userId,
                Status: "ALL",
                Priority: "all");

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastStatus.Should().BeNull();
            _repository.LastPriority.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_PassWorkspaceId_When_WorkspaceIdProvided()
        {
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var query = new GetMyTasksQuery(userId, WorkspaceId: workspaceId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastUserId.Should().Be(userId);
            _repository.LastWorkspaceId.Should().Be(workspaceId);
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public List<TaskItem> TasksToReturn { get; } = new();

            public Guid LastUserId { get; private set; }
            public Guid? LastWorkspaceId { get; private set; }
            public string? LastType { get; private set; }
            public string? LastSearch { get; private set; }
            public TaskStatus? LastStatus { get; private set; }
            public Priority? LastPriority { get; private set; }
            public string? LastTag { get; private set; }
            public int LastPage { get; private set; }
            public int LastPageSize { get; private set; }

            public Task<PaginationResult<TaskItem>> GetMyAsync(
                Guid userId,
                Guid? workspaceId,
                string? type,
                string? search,
                TaskStatus? status,
                Priority? priority,
                string? tag,
                int page,
                int pageSize,
                CancellationToken cancellationToken)
            {
                LastUserId = userId;
                LastWorkspaceId = workspaceId;
                LastType = type;
                LastSearch = search;
                LastStatus = status;
                LastPriority = priority;
                LastTag = tag;
                LastPage = page;
                LastPageSize = pageSize;

                var result = PaginationResult<TaskItem>.Create(TasksToReturn, page, pageSize, TasksToReturn.Count);
                return Task.FromResult(result);
            }

            public Task<PaginationResult<TaskItem>> GetAllAsync(
                Guid? workspaceId,
                string? search,
                TaskStatus? status,
                Priority? priority,
                Guid? assigneeId,
                Guid? createdById,
                string? tag,
                int page,
                int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<TaskItem?>(null);
            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task Handle_Should_PopulateAssignees_ViaBatch_When_UsersApiClientProvided()
        {
            var userId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task with Assignee", "Desc", Priority.Medium, null, assigneeId, userId);
            _repository.TasksToReturn.Add(task);

            var fakeUsersClient = new FakeUsersApiClient();
            fakeUsersClient.Users[assigneeId] = new UserResult(assigneeId, "assigned@test.com", "assignee", "Assignee User");

            var handler = new GetMyTasksQueryHandler(_repository, fakeUsersClient);
            var result = await handler.Handle(new GetMyTasksQuery(userId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Assignee.Should().NotBeNull();
            result.Value.Items[0].Assignee!.Id.Should().Be(assigneeId);
            result.Value.Items[0].Assignee!.UserName.Should().Be("assignee");
            fakeUsersClient.BatchCallCount.Should().Be(1);
        }

        private sealed class FakeUsersApiClient : IUsersApiClient
        {
            public Dictionary<Guid, UserResult> Users { get; } = new();
            public int BatchCallCount { get; private set; }

            public Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            {
                Users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }

            public Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                int page = 1,
                int pageSize = 100,
                CancellationToken cancellationToken = default)
            {
                BatchCallCount++;
                IReadOnlyDictionary<Guid, UserResult> result = Users
                    .Where(kvp => userIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return Task.FromResult(result);
            }
        }
    }
}
