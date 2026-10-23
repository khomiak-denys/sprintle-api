using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.Cancel;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.Cancel
{
    public class CancelTaskCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly CancelTaskCommandHandler _handler;

        public CancelTaskCommandHandlerTests()
        {
            _handler = new CancelTaskCommandHandler(
                _uow,
                _taskRepository,
                _workspaceRepository,
                _usersApiClient,
                _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var command = new CancelTaskCommand(Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_CallerIsNotCreatorAndNotAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var strangerId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            var command = new CancelTaskCommand(task.Id, strangerId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Message.Should().Be("Only an administrator or the task creator can cancel this task.");
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_CancelTaskAndPublishEvent_When_CallerIsCreator()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task 1", null, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[assigneeId] = new UserResult(assigneeId, "assignee@example.com", "assignee_user", "Assignee Name");

            var command = new CancelTaskCommand(task.Id, creatorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Cancelled);
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskStatusChanged);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskStatusChanged>().Single();
            publishedEvent.TaskId.Should().Be(task.Id);
            publishedEvent.OldStatus.Should().Be("InProgress");
            publishedEvent.NewStatus.Should().Be("Cancelled");
            publishedEvent.ChangedByUserId.Should().Be(creatorId);
            publishedEvent.ChangedByUserName.Should().Be("Creator Name");
            publishedEvent.AssigneeUserId.Should().Be(assigneeId);
            publishedEvent.AssigneeEmail.Should().Be("assignee@example.com");
            publishedEvent.AssigneeName.Should().Be("Assignee Name");
            publishedEvent.WorkspaceName.Should().Be("Dev Workspace");
        }

        [Fact]
        public async Task Handle_Should_CancelTaskAndPublishEvent_When_CallerIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task Admin", null, Priority.High, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[adminId] = new UserResult(adminId, "admin@example.com", "admin_user", "Admin Name");
            _usersApiClient.Users[assigneeId] = new UserResult(assigneeId, "assignee@example.com", "assignee_user", "Assignee Name");

            var command = new CancelTaskCommand(task.Id, adminId, true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Cancelled);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskStatusChanged);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskStatusChanged>().Single();
            publishedEvent.ChangedByUserId.Should().Be(adminId);
            publishedEvent.ChangedByUserName.Should().Be("Admin Name");
            publishedEvent.OldStatus.Should().Be("InProgress");
            publishedEvent.NewStatus.Should().Be("Cancelled");
        }

        [Fact]
        public async Task Handle_Should_CancelTaskAndPublishEventWithNullAssignee_When_TaskIsUnassigned()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Unassigned Task", null, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");

            var command = new CancelTaskCommand(task.Id, creatorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Cancelled);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskStatusChanged);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskStatusChanged>().Single();
            publishedEvent.OldStatus.Should().Be("Todo");
            publishedEvent.NewStatus.Should().Be("Cancelled");
            publishedEvent.AssigneeUserId.Should().BeNull();
            publishedEvent.AssigneeEmail.Should().BeNull();
            publishedEvent.AssigneeName.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TaskStatusCannotBeCancelled()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Done Task", null, Priority.Medium, null, assigneeId, creatorId);
            task.SubmitForReview();
            task.Approve();
            _taskRepository.TaskToReturn = task;

            var command = new CancelTaskCommand(task.Id, creatorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
            _uow.SaveCount.Should().Be(0);
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
                Task.FromResult(true);
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public TaskItem? TaskToReturn { get; set; }

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
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
