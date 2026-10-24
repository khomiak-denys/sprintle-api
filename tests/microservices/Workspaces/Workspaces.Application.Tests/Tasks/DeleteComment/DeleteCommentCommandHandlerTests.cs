using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DeleteComment;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.DeleteComment
{
    public class DeleteCommentCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly DeleteCommentCommandHandler _handler;

        public DeleteCommentCommandHandlerTests()
        {
            _handler = new DeleteCommentCommandHandler(_uow, _taskRepository);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_CommentDoesNotExist()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            _taskRepository.TaskToReturn = task;

            var command = new DeleteCommentCommand(task.Id, Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_CommentBelongsToDifferentTask()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            _taskRepository.TaskToReturn = task;

            var otherTaskId = Guid.NewGuid();
            var comment = TaskComment.Create(otherTaskId, Guid.NewGuid(), "Comment on other task");
            _taskRepository.CommentToReturn = comment;

            var command = new DeleteCommentCommand(task.Id, comment.Id, comment.AuthorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotAuthorNorAdmin()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var comment = TaskComment.Create(task.Id, authorId, "Comment text");

            _taskRepository.TaskToReturn = task;
            _taskRepository.CommentToReturn = comment;

            var command = new DeleteCommentCommand(task.Id, comment.Id, otherUserId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            _taskRepository.RemovedComment.Should().BeNull();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_RemoveCommentAndSave_When_UserIsAuthor()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var comment = TaskComment.Create(task.Id, authorId, "Comment text");

            _taskRepository.TaskToReturn = task;
            _taskRepository.CommentToReturn = comment;

            var command = new DeleteCommentCommand(task.Id, comment.Id, authorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _taskRepository.RemovedComment.Should().Be(comment);
            _uow.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_RemoveCommentAndSave_When_UserIsAdmin()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var comment = TaskComment.Create(task.Id, authorId, "Comment text");

            _taskRepository.TaskToReturn = task;
            _taskRepository.CommentToReturn = comment;

            var command = new DeleteCommentCommand(task.Id, comment.Id, adminId, true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _taskRepository.RemovedComment.Should().Be(comment);
            _uow.SaveCount.Should().Be(1);
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
            public TaskComment? CommentToReturn { get; set; }
            public TaskComment? RemovedComment { get; private set; }

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken) =>
                Task.FromResult(CommentToReturn?.Id == commentId ? CommentToReturn : null);

            public Task RemoveCommentAsync(TaskComment comment, CancellationToken cancellationToken)
            {
                RemovedComment = comment;
                return Task.CompletedTask;
            }

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
    }
}
