using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.LogTime;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.LogTime
{
    public class LogTaskTimeCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly LogTaskTimeCommandHandler _handler;

        public LogTaskTimeCommandHandlerTests()
        {
            _handler = new LogTaskTimeCommandHandler(_uow, _taskRepository);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var command = new LogTaskTimeCommand(Guid.NewGuid(), Guid.NewGuid(), 60, "Work done", DateOnly.FromDateTime(DateTime.UtcNow));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_LogTimeAndSave_When_ValidRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, userId, userId);
            _taskRepository.TaskToReturn = task;

            var command = new LogTaskTimeCommand(task.Id, userId, 90, "Finished feature", DateOnly.FromDateTime(DateTime.UtcNow));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.TimeLogs.Should().ContainSingle(tl => tl.MinutesSpent == 90 && tl.Description == "Finished feature" && tl.UserId == userId);
            _taskRepository.AddedTimeLogs.Should().ContainSingle(tl => tl.MinutesSpent == 90 && tl.Description == "Finished feature");
            _uow.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotAssignedUser()
        {
            // Arrange
            var assigneeId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, assigneeId, Guid.NewGuid());
            _taskRepository.TaskToReturn = task;

            var command = new LogTaskTimeCommand(task.Id, otherUserId, 60, "Work by other user", DateOnly.FromDateTime(DateTime.UtcNow));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Message.Should().Be("Only the assigned user can log time on this task.");
            _uow.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_TaskHasNoAssignee()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, userId);
            _taskRepository.TaskToReturn = task;

            var command = new LogTaskTimeCommand(task.Id, userId, 60, "Work on unassigned task", DateOnly.FromDateTime(DateTime.UtcNow));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Message.Should().Be("Only the assigned user can log time on this task.");
            _uow.SaveCount.Should().Be(0);
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
            public List<TimeLog> AddedTimeLogs { get; } = new();

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task AddTimeLogAsync(TimeLog timeLog, CancellationToken cancellationToken)
            {
                AddedTimeLogs.Add(timeLog);
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
