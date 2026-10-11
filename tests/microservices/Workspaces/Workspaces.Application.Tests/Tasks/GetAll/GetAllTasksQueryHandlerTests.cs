using DomainFramework;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetAll;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetAll
{
    public class GetAllTasksQueryHandlerTests
    {
        private readonly FakeTaskRepository _repository = new();
        private readonly GetAllTasksQueryHandler _handler;

        public GetAllTasksQueryHandlerTests()
        {
            _handler = new GetAllTasksQueryHandler(_repository);
        }

        [Fact]
        public async Task Handle_Should_ReturnAllTasks_When_QueryHasDefaults()
        {
            var workspaceId = Guid.NewGuid();
            var task = TaskItem.Create(workspaceId, "Task 1", "Desc", Priority.Medium, null, null, Guid.NewGuid());
            _repository.TasksToReturn.Add(task);

            var query = new GetAllTasksQuery();
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Title.Should().Be("Task 1");
            _repository.LastWorkspaceId.Should().BeNull();
            _repository.LastSearch.Should().BeNull();
            _repository.LastStatus.Should().BeNull();
            _repository.LastPriority.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_PassWorkspaceId_When_Provided()
        {
            var workspaceId = Guid.NewGuid();
            var query = new GetAllTasksQuery(WorkspaceId: workspaceId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastWorkspaceId.Should().Be(workspaceId);
        }

        [Fact]
        public async Task Handle_Should_ParseStatusAndPriority_When_Provided()
        {
            var query = new GetAllTasksQuery(
                Search: "fix bug",
                Status: "InProgress",
                Priority: "High",
                Tag: "backend",
                Page: 2,
                PageSize: 20);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastSearch.Should().Be("fix bug");
            _repository.LastStatus.Should().Be(TaskStatus.InProgress);
            _repository.LastPriority.Should().Be(Priority.High);
            _repository.LastTag.Should().Be("backend");
            _repository.LastPage.Should().Be(2);
            _repository.LastPageSize.Should().Be(20);
        }

        [Fact]
        public async Task Handle_Should_TreatAllAsNull_For_StatusAndPriority()
        {
            var query = new GetAllTasksQuery(
                Status: "all",
                Priority: "ALL");

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastStatus.Should().BeNull();
            _repository.LastPriority.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_SetEffectiveAssigneeId_When_TypeIsAssigned()
        {
            var userId = Guid.NewGuid();
            var query = new GetAllTasksQuery(
                Type: "assigned",
                RequestingUserId: userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastAssigneeId.Should().Be(userId);
            _repository.LastCreatedById.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_SetEffectiveCreatedById_When_TypeIsCreated()
        {
            var userId = Guid.NewGuid();
            var query = new GetAllTasksQuery(
                Type: "created",
                RequestingUserId: userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _repository.LastCreatedById.Should().Be(userId);
            _repository.LastAssigneeId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_PopulateAssignees_ViaBatch_When_UsersApiClientProvided()
        {
            var assigneeId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task with Assignee", "Desc", Priority.Low, null, assigneeId, Guid.NewGuid());
            _repository.TasksToReturn.Add(task);

            var fakeUsersClient = new FakeUsersApiClient();
            fakeUsersClient.Users[assigneeId] = new UserResult(assigneeId, "user@test.com", "testuser", "Test User");

            var handler = new GetAllTasksQueryHandler(_repository, fakeUsersClient);
            var result = await handler.Handle(new GetAllTasksQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Assignee.Should().NotBeNull();
            result.Value.Items[0].Assignee!.Id.Should().Be(assigneeId);
            result.Value.Items[0].Assignee!.UserName.Should().Be("testuser");
            fakeUsersClient.BatchCallCount.Should().Be(1);
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public List<TaskItem> TasksToReturn { get; } = new();

            public Guid? LastWorkspaceId { get; private set; }
            public string? LastSearch { get; private set; }
            public TaskStatus? LastStatus { get; private set; }
            public Priority? LastPriority { get; private set; }
            public Guid? LastAssigneeId { get; private set; }
            public Guid? LastCreatedById { get; private set; }
            public string? LastTag { get; private set; }
            public int LastPage { get; private set; }
            public int LastPageSize { get; private set; }

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
                CancellationToken cancellationToken)
            {
                LastWorkspaceId = workspaceId;
                LastSearch = search;
                LastStatus = status;
                LastPriority = priority;
                LastAssigneeId = assigneeId;
                LastCreatedById = createdById;
                LastTag = tag;
                LastPage = page;
                LastPageSize = pageSize;

                var result = PaginationResult<TaskItem>.Create(TasksToReturn, page, pageSize, TasksToReturn.Count);
                return Task.FromResult(result);
            }

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
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<TaskItem?>(null);
            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
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
