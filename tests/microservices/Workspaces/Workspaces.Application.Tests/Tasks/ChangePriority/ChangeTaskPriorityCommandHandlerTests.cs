using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.ChangePriority;
using Workspaces.Domain.Tasks;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.ChangePriority
{
    public class ChangeTaskPriorityCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly ChangeTaskPriorityCommandHandler _handler;

        public ChangeTaskPriorityCommandHandlerTests()
        {
            _handler = new ChangeTaskPriorityCommandHandler(_uow, _taskRepository);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var command = new ChangeTaskPriorityCommand(Guid.NewGuid(), Priority.High, Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotCreatorAndNotAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var otherUserId = Guid.NewGuid();
            var command = new ChangeTaskPriorityCommand(task.Id, Priority.High, otherUserId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            _uow.SaveCount.Should().Be(0);
            task.Priority.Should().Be(Priority.Low);
        }

        [Fact]
        public async Task Handle_Should_UpdatePriority_When_UserIsCreator()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var command = new ChangeTaskPriorityCommand(task.Id, Priority.Critical, creatorId, false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _uow.SaveCount.Should().Be(1);
            task.Priority.Should().Be(Priority.Critical);
        }

        [Fact]
        public async Task Handle_Should_UpdatePriority_When_UserIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, TaskType.Task, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var adminId = Guid.NewGuid();
            var command = new ChangeTaskPriorityCommand(task.Id, Priority.High, adminId, true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _uow.SaveCount.Should().Be(1);
            task.Priority.Should().Be(Priority.High);
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCount++;
                return Task.FromResult(1);
            }
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public TaskItem? TaskToReturn { get; set; }

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<PaginationResult<TaskItem>> GetAllAsync(
                Guid? workspaceId, string? search, Domain.Tasks.TaskStatus? status, Priority? priority,
                Guid? assigneeId, Guid? createdById, string? tag, int page, int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<PaginationResult<TaskItem>> GetMyAsync(
                Guid userId, Guid? workspaceId, string? type, string? search, Domain.Tasks.TaskStatus? status,
                Priority? priority, string? tag, int page, int pageSize, CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));
        }
    }
}
