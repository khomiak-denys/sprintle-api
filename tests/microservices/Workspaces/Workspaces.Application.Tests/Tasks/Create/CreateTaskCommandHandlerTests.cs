using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.Create;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tags;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.Create
{
    public class CreateTaskCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeTagRepository _tagRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly CreateTaskCommandHandler _handler;

        public CreateTaskCommandHandlerTests()
        {
            _handler = new CreateTaskCommandHandler(
                _uow,
                _taskRepository,
                _workspaceRepository,
                _tagRepository,
                _usersApiClient,
                _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_CreatorIsNotMemberAndNotAdmin()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();

            var command = new CreateTaskCommand(
                workspaceId,
                creatorId,
                "Title",
                "Description",
                Priority.Medium,
                null,
                null,
                Enumerable.Empty<string>(),
                false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Message.Should().Be("You must be a member of the workspace to create a task in it.");
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnInvalidArgument_When_AssigneeIsNotMember()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var nonMemberAssigneeId = Guid.NewGuid();

            _workspaceRepository.Members.Add((workspaceId, creatorId));

            var command = new CreateTaskCommand(
                workspaceId,
                creatorId,
                "Title",
                "Description",
                Priority.Medium,
                null,
                nonMemberAssigneeId,
                Enumerable.Empty<string>(),
                false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
            result.Error.Message.Should().Be("The assigned user must be a member of the workspace.");
            _uow.SaveCount.Should().Be(0);
        }

        private static readonly string[] TestTags = ["backend", "urgent"];

        [Fact]
        public async Task Handle_Should_CreateTaskAndSave_When_ValidMemberAndAssignee()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var workspace = Workspace.Create("Main Team", creatorId, null);
            _workspaceRepository.Workspaces[workspaceId] = workspace;
            _workspaceRepository.Members.Add((workspaceId, creatorId));
            _workspaceRepository.Members.Add((workspaceId, assigneeId));

            _usersApiClient.Users[assigneeId] = new UserResult(assigneeId, "assignee@example.com", "assignee", "Assignee Full");
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator", "Creator Full");

            var command = new CreateTaskCommand(
                workspaceId,
                creatorId,
                "Valid Task",
                "Description",
                Priority.High,
                DateTime.UtcNow.AddDays(5),
                assigneeId,
                TestTags,
                false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
            var createdTask = _taskRepository.AddedTasks.Single(t => t.Id == result.Value);
            createdTask.Status.Should().Be(TaskStatus.InProgress);
            createdTask.AssigneeId.Should().Be(assigneeId);
            createdTask.Number.Should().Be("DMS-1");
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskAssigned);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskAssigned>().Single();
            publishedEvent.TaskId.Should().Be(result.Value);
            publishedEvent.TaskTitle.Should().Be("Valid Task");
            publishedEvent.TaskNumber.Should().Be("DMS-1");
            publishedEvent.AssigneeUserId.Should().Be(assigneeId);
            publishedEvent.AssigneeEmail.Should().Be("assignee@example.com");
            publishedEvent.AssigneeName.Should().Be("Assignee Full");
            publishedEvent.AssignedByUserId.Should().Be(creatorId);
            publishedEvent.AssignedByName.Should().Be("Creator Full");
            publishedEvent.WorkspaceName.Should().Be("Main Team");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_WorkspaceNotFoundForTaskNumberReservation()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            _workspaceRepository.Members.Add((workspaceId, creatorId));
            _workspaceRepository.ReturnNullTaskNumber = true;

            var command = new CreateTaskCommand(
                workspaceId,
                creatorId,
                "Title",
                "Description",
                Priority.Medium,
                null,
                null,
                Enumerable.Empty<string>(),
                false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_CreateTaskAndSave_When_AdminEvenIfNotMember()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var adminId = Guid.NewGuid();

            var command = new CreateTaskCommand(
                workspaceId,
                adminId,
                "Admin Created Task",
                null,
                Priority.Low,
                null,
                null,
                Enumerable.Empty<string>(),
                true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
            var createdTask = _taskRepository.AddedTasks.Single(t => t.Id == result.Value);
            createdTask.Status.Should().Be(TaskStatus.Todo);
            createdTask.AssigneeId.Should().BeNull();
            _uow.SaveCount.Should().Be(1);
            _eventPublisher.PublishedEvents.Should().BeEmpty();
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
            {
                SaveCount++;
                return Task.FromResult(1);
            }
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public HashSet<(Guid WorkspaceId, Guid UserId)> Members { get; } = new();
            public Dictionary<Guid, Workspace> Workspaces { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(Workspaces.TryGetValue(id, out var ws) ? ws : null);

            public Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(Array.Empty<Workspace>(), page, pageSize, 0));

            public Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Workspace>.Create(Array.Empty<Workspace>(), page, pageSize, 0));

            public Task AddAsync(Workspace workspace, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(Workspace workspace, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
                Task.FromResult(Members.Contains((workspaceId, userId)));

            public int NextTaskNumber { get; set; } = 1;
            public bool ReturnNullTaskNumber { get; set; }

            public Task<string?> ReserveNextTaskNumberAsync(Guid workspaceId, CancellationToken cancellationToken) =>
                Task.FromResult(ReturnNullTaskNumber ? null : $"DMS-{NextTaskNumber++}");
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public List<TaskItem> AddedTasks { get; } = new();

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult<TaskItem?>(null);

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
            {
                AddedTasks.Add(task);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<PaginationResult<TaskItem>> GetAllAsync(
                Guid? workspaceId, string? search, TaskStatus? status, Priority? priority,
                Guid? assigneeId, Guid? createdById, string? tag, int page, int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<PaginationResult<TaskItem>> GetMyAsync(
                Guid userId, Guid? workspaceId, string? type, string? search, TaskStatus? status,
                Priority? priority, string? tag, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));
        }

        private sealed class FakeTagRepository : ITagRepository
        {
            public List<Tag> Tags { get; } = new();

            public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
                Task.FromResult(Tags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

            public Task AddAsync(Tag tag, CancellationToken cancellationToken)
            {
                Tags.Add(tag);
                return Task.CompletedTask;
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
