using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.AddComment;
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
        private readonly AddCommentCommandHandler _handler;

        public AddCommentCommandHandlerTests()
        {
            _handler = new AddCommentCommandHandler(_uow, _taskRepository, _workspaceRepository);
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
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, assigneeId, creatorId);
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
        public async Task Handle_Should_AddCommentAndSave_When_UserIsCreator()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Members.Add((task.WorkspaceId, creatorId));

            var command = new AddCommentCommand(task.Id, creatorId, false, "Comment by creator");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
            task.Comments.Should().ContainSingle(c => c.Id == result.Value && c.Content == "Comment by creator");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Id == result.Value);
            _uow.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_AddCommentAndSave_When_UserIsAssignee()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Members.Add((task.WorkspaceId, assigneeId));

            var command = new AddCommentCommand(task.Id, assigneeId, false, "Comment by assignee");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by assignee");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by assignee");
            _uow.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_AddCommentAndSave_When_UserIsWorkspaceMember()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;
            _workspaceRepository.Members.Add((task.WorkspaceId, memberId));

            var command = new AddCommentCommand(task.Id, memberId, false, "Comment by member");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by member");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by member");
            _uow.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_AddCommentAndSave_When_UserIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var command = new AddCommentCommand(task.Id, adminId, true, "Comment by admin");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().ContainSingle(c => c.Content == "Comment by admin");
            _taskRepository.AddedComments.Should().ContainSingle(c => c.Content == "Comment by admin");
            _uow.SaveCount.Should().Be(1);
        }

        private sealed class FakeWorkspaceRepository : IWorkspaceRepository
        {
            public HashSet<(Guid WorkspaceId, Guid UserId)> Members { get; } = new();

            public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult<Workspace?>(null);

            public Task<PaginationResult<Domain.Workspaces.Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Domain.Workspaces.Workspace>.Create(Array.Empty<Domain.Workspaces.Workspace>(), page, pageSize, 0));

            public Task<PaginationResult<Domain.Workspaces.Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<Domain.Workspaces.Workspace>.Create(Array.Empty<Domain.Workspaces.Workspace>(), page, pageSize, 0));

            public Task AddAsync(Domain.Workspaces.Workspace workspace, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(Domain.Workspaces.Workspace workspace, CancellationToken cancellationToken) => Task.CompletedTask;

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
    }
}
