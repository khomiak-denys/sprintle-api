using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.Assign;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.Assign
{
    public class AssignTaskCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly AssignTaskCommandHandler _handler;

        public AssignTaskCommandHandlerTests()
        {
            _handler = new AssignTaskCommandHandler(_uow, _taskRepository, _workspaceRepository, _usersApiClient, _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var command = new AssignTaskCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_UserDoesNotExist()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var nonExistentUserId = Guid.NewGuid();
            var command = new AssignTaskCommand(task.Id, nonExistentUserId, creatorId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnInvalidArgument_When_UserIsNotWorkspaceMember()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var nonMemberUserId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            _usersApiClient.Users[nonMemberUserId] = new UserResult(nonMemberUserId, "user@example.com", "user", "User Name");

            var command = new AssignTaskCommand(task.Id, nonMemberUserId, creatorId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
            result.Error.Message.Should().Be("The assigned user must be a member of the workspace.");
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_AssignTaskAndSave_When_Valid()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var workspace = Workspace.Create("Backend Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "Task", "Task Desc", Priority.High, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _workspaceRepository.Members.Add((workspace.Id, memberId));
            _usersApiClient.Users[memberId] = new UserResult(memberId, "member@example.com", "member", "Member Name");
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator", "Creator Name");

            var command = new AssignTaskCommand(task.Id, memberId, creatorId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.AssigneeId.Should().Be(memberId);
            task.Status.Should().Be(TaskStatus.InProgress);
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskAssigned);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskAssigned>().Single();
            publishedEvent.TaskId.Should().Be(task.Id);
            publishedEvent.TaskTitle.Should().Be("Task");
            publishedEvent.TaskDescription.Should().Be("Task Desc");
            publishedEvent.WorkspaceId.Should().Be(workspace.Id);
            publishedEvent.WorkspaceName.Should().Be("Backend Workspace");
            publishedEvent.AssigneeUserId.Should().Be(memberId);
            publishedEvent.AssigneeEmail.Should().Be("member@example.com");
            publishedEvent.AssigneeName.Should().Be("Member Name");
            publishedEvent.AssignedByUserId.Should().Be(creatorId);
            publishedEvent.AssignedByName.Should().Be("Creator Name");
            publishedEvent.Priority.Should().Be("High");
        }

        [Fact]
        public async Task Handle_Should_PublishTaskAssignedWithFallbackNames_When_UsersHaveNoFullName()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var task = TaskItem.Create(workspaceId, "Simple Task", null, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            _workspaceRepository.Members.Add((workspaceId, memberId));
            _usersApiClient.Users[memberId] = new UserResult(memberId, "member@example.com", "memberusername", null);
            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creatorusername", null);

            var command = new AssignTaskCommand(task.Id, memberId, creatorId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskAssigned>().Single();
            publishedEvent.AssigneeName.Should().Be("memberusername");
            publishedEvent.AssignedByName.Should().Be("creatorusername");
            publishedEvent.WorkspaceName.Should().Be("Workspace");
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
