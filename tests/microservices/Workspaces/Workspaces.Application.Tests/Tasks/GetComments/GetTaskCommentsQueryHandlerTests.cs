using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetComments;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetComments
{
    public class GetTaskCommentsQueryHandlerTests
    {
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeWorkspaceRepository _workspaceRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly GetTaskCommentsQueryHandler _handler;

        public GetTaskCommentsQueryHandlerTests()
        {
            _handler = new GetTaskCommentsQueryHandler(_taskRepository, _workspaceRepository, _usersApiClient);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var query = new GetTaskCommentsQuery(Guid.NewGuid(), Guid.NewGuid(), false, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotWorkspaceMemberAndNotAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            var query = new GetTaskCommentsQuery(task.Id, otherUserId, false, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyPage_When_NoCommentsExist()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _taskRepository.CommentsToReturn = PaginationResult<TaskComment>.Create(Array.Empty<TaskComment>(), 1, 20, 0);
            _workspaceRepository.Members.Add((task.WorkspaceId, creatorId));

            var query = new GetTaskCommentsQuery(task.Id, creatorId, false, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnCommentsWithResolvedAuthors_When_CommentsExist()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Members.Add((task.WorkspaceId, creatorId));

            var comment = TaskComment.Create(task.Id, authorId, "Great job on this!");
            _taskRepository.CommentsToReturn = PaginationResult<TaskComment>.Create(new[] { comment }, 1, 20, 1);

            _usersApiClient.Users[authorId] = new UserResult(authorId, "author@example.com", "author", "Author Name");

            var query = new GetTaskCommentsQuery(task.Id, creatorId, false, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
            var returnedComment = result.Value.Items[0];
            returnedComment.Id.Should().Be(comment.Id);
            returnedComment.AuthorId.Should().Be(authorId);
            returnedComment.Content.Should().Be("Great job on this!");
            returnedComment.Author.Should().NotBeNull();
            returnedComment.Author!.UserName.Should().Be("author");
        }

        [Fact]
        public async Task Handle_Should_ReturnComments_When_UserIsWorkspaceMember()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Members.Add((task.WorkspaceId, memberId));

            var comment = TaskComment.Create(task.Id, authorId, "Member viewing this!");
            _taskRepository.CommentsToReturn = PaginationResult<TaskComment>.Create(new[] { comment }, 1, 20, 1);
            _usersApiClient.Users[authorId] = new UserResult(authorId, "author@example.com", "author", "Author");

            var query = new GetTaskCommentsQuery(task.Id, memberId, false, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_Should_AllowAdminToAccess_When_UserIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _taskRepository.CommentsToReturn = PaginationResult<TaskComment>.Create(Array.Empty<TaskComment>(), 1, 20, 0);

            var query = new GetTaskCommentsQuery(task.Id, adminId, true, 1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public HashSet<(Guid WorkspaceId, Guid UserId)> Members { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult<Workspace?>(null);

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
            public PaginationResult<TaskComment>? CommentsToReturn { get; set; }

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task<PaginationResult<TaskComment>> GetCommentsByTaskIdAsync(
                Guid taskId, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(CommentsToReturn ?? PaginationResult<TaskComment>.Create(Array.Empty<TaskComment>(), page, pageSize, 0));

            public Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken) =>
                Task.FromResult<TaskComment?>(null);

            public Task RemoveCommentAsync(TaskComment comment, CancellationToken cancellationToken) =>
                Task.CompletedTask;

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<DomainFramework.PaginationResult<TaskItem>> GetAllAsync(
                Guid? workspaceId, string? search, TaskStatus? status, Priority? priority,
                Guid? assigneeId, Guid? createdById, string? tag, int page, int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(DomainFramework.PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<DomainFramework.PaginationResult<TaskItem>> GetMyAsync(
                Guid userId, Guid? workspaceId, string? type, string? search, TaskStatus? status,
                Priority? priority, string? tag, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(DomainFramework.PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));
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
    }
}
