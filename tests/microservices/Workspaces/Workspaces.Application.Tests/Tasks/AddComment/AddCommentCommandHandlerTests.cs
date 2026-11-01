using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.AddComment;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.AddComment
{
    public class AddCommentCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly AddCommentCommandHandler _handler;

        public AddCommentCommandHandlerTests()
        {
            _handler = new AddCommentCommandHandler(
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
            var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), false, "A comment");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotWorkspaceMemberAndNotAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            var command = new AddCommentCommand(task.Id, unauthorizedUserId, false, "Unauthorized comment");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_AddCommentSaveAndPublishEvent_When_UserIsCreator()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _workspaceRepository.Members.Add((task.WorkspaceId, creatorId));

            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[assigneeId] = new UserResult(assigneeId, "assignee@example.com", "assignee_user", "Assignee Name");

            var command = new AddCommentCommand(task.Id, creatorId, false, "Comment by creator");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
            task.Comments.Should().ContainSingle(c => c.Id == result.Value && c.Content == "Comment by creator");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Id == result.Value);
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskCommentAdded);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskCommentAdded>().Single();
            publishedEvent.TaskId.Should().Be(task.Id);
            publishedEvent.TaskTitle.Should().Be(task.Title);
            publishedEvent.WorkspaceName.Should().Be("Dev Workspace");
            publishedEvent.CommentId.Should().Be(result.Value);
            publishedEvent.CommentContent.Should().Be("Comment by creator");
            publishedEvent.CommentAuthorUserId.Should().Be(creatorId);
            publishedEvent.CommentAuthorName.Should().Be("Creator Name");
            publishedEvent.CreatedByUserId.Should().Be(creatorId);
            publishedEvent.CreatedByEmail.Should().Be("creator@example.com");
            publishedEvent.AssigneeUserId.Should().Be(assigneeId);
            publishedEvent.AssigneeEmail.Should().Be("assignee@example.com");
        }

        [Fact]
        public async Task Handle_Should_AddCommentSaveAndPublishEvent_When_UserIsAssignee()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _workspaceRepository.Members.Add((task.WorkspaceId, assigneeId));

            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[assigneeId] = new UserResult(assigneeId, "assignee@example.com", "assignee_user", "Assignee Name");

            var command = new AddCommentCommand(task.Id, assigneeId, false, "Comment by assignee");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by assignee");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by assignee");
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskCommentAdded);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskCommentAdded>().Single();
            publishedEvent.CommentAuthorUserId.Should().Be(assigneeId);
            publishedEvent.CommentAuthorName.Should().Be("Assignee Name");
        }

        [Fact]
        public async Task Handle_Should_AddCommentSaveAndPublishEvent_When_UserIsWorkspaceMember()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Workspaces[workspace.Id] = workspace;
            _workspaceRepository.Members.Add((task.WorkspaceId, memberId));

            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[memberId] = new UserResult(memberId, "member@example.com", "member_user", "Member Name");

            var command = new AddCommentCommand(task.Id, memberId, false, "Comment by member");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by member");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by member");
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskCommentAdded);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskCommentAdded>().Single();
            publishedEvent.CommentAuthorUserId.Should().Be(memberId);
            publishedEvent.AssigneeUserId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_AddCommentSaveAndPublishEvent_When_UserIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var workspace = Workspace.Create("Dev Workspace", creatorId, null);
            var task = TaskItem.Create(workspace.Id, "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Workspaces[workspace.Id] = workspace;

            _usersApiClient.Users[creatorId] = new UserResult(creatorId, "creator@example.com", "creator_user", "Creator Name");
            _usersApiClient.Users[adminId] = new UserResult(adminId, "admin@example.com", "admin_user", "Admin Name");

            var command = new AddCommentCommand(task.Id, adminId, true, "Comment by admin");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by admin");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by admin");
            _uow.SaveCount.Should().Be(1);

            _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is TaskCommentAdded);
            var publishedEvent = _eventPublisher.PublishedEvents.OfType<TaskCommentAdded>().Single();
            publishedEvent.CommentAuthorUserId.Should().Be(adminId);
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public Dictionary<Guid, Workspace> Workspaces { get; } = new();
            public HashSet<(Guid WorkspaceId, Guid UserId)> Members { get; } = new();

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

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
            {
                SaveCount++;
                return Task.FromResult(1);
            }
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public TaskItem? TaskToReturn { get; set; }
            public List<TaskComment> AddedComments { get; } = new();

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken)
            {
                AddedComments.Add(comment);
                return Task.CompletedTask;
            }
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
