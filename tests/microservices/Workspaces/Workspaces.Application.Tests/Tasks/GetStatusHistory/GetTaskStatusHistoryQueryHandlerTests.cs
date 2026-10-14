using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Tasks.GetStatusHistory;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Tasks.Events;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetStatusHistory
{
    public class GetTaskStatusHistoryQueryHandlerTests
    {
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeTaskEventStore _eventStore = new();
        private readonly GetTaskStatusHistoryQueryHandler _handler;

        public GetTaskStatusHistoryQueryHandlerTests()
        {
            _handler = new GetTaskStatusHistoryQueryHandler(_taskRepository, _eventStore);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var query = new GetTaskStatusHistoryQuery(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyHistory_When_TaskHasNoEvents()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            _taskRepository.Tasks[task.Id] = task;
            var query = new GetTaskStatusHistoryQuery(task.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_ReturnProjectedHistory_When_TaskExistsWithEvents()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var task = TaskItem.Create(workspaceId, "Task", null, Priority.High, null, null, creatorId);
            _taskRepository.Tasks[taskId] = task;

            var events = new List<IDomainEvent>
            {
                new TaskCreatedDomainEvent(taskId, workspaceId, "Task", null, Priority.High, null, null, creatorId, TaskStatus.Todo),
                new TaskAssignedDomainEvent(taskId, assigneeId, TaskStatus.Todo, TaskStatus.InProgress),
                new TaskSubmittedForReviewDomainEvent(taskId, TaskStatus.InProgress, TaskStatus.InReview),
                new TaskApprovedDomainEvent(taskId, TaskStatus.InReview, TaskStatus.Done),
                new TaskClosedDomainEvent(taskId, TaskStatus.Done, TaskStatus.Closed)
            };
            _eventStore.Events[taskId] = events;

            var query = new GetTaskStatusHistoryQuery(taskId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(5);

            result.Value[0].EventType.Should().Be(nameof(TaskCreatedDomainEvent));
            result.Value[0].NewStatus.Should().Be("Todo");
            result.Value[0].Version.Should().Be(1);

            result.Value[1].EventType.Should().Be(nameof(TaskAssignedDomainEvent));
            result.Value[1].PreviousStatus.Should().Be("Todo");
            result.Value[1].NewStatus.Should().Be("InProgress");
            result.Value[1].Version.Should().Be(2);

            result.Value[2].EventType.Should().Be(nameof(TaskSubmittedForReviewDomainEvent));
            result.Value[2].PreviousStatus.Should().Be("InProgress");
            result.Value[2].NewStatus.Should().Be("InReview");

            result.Value[3].EventType.Should().Be(nameof(TaskApprovedDomainEvent));
            result.Value[3].PreviousStatus.Should().Be("InReview");
            result.Value[3].NewStatus.Should().Be("Done");

            result.Value[4].EventType.Should().Be(nameof(TaskClosedDomainEvent));
            result.Value[4].PreviousStatus.Should().Be("Done");
            result.Value[4].NewStatus.Should().Be("Closed");
            result.Value[4].Version.Should().Be(5);
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public Dictionary<Guid, TaskItem> Tasks { get; } = new();

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            {
                Tasks.TryGetValue(id, out var task);
                return Task.FromResult(task);
            }

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
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

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

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
            {
                Tasks[task.Id] = task;
                return Task.CompletedTask;
            }

            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken)
            {
                Tasks.Remove(task.Id);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTaskEventStore : ITaskEventStore
        {
            public Dictionary<Guid, List<IDomainEvent>> Events { get; } = new();

            public Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid taskId, CancellationToken cancellationToken = default)
            {
                if (Events.TryGetValue(taskId, out var list))
                {
                    return Task.FromResult<IReadOnlyList<IDomainEvent>>(list.AsReadOnly());
                }

                return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
            }

            public Task AppendEventsAsync(
                Guid taskId,
                int expectedVersion,
                IReadOnlyCollection<IDomainEvent> events,
                CancellationToken cancellationToken = default)
            {
                if (!Events.TryGetValue(taskId, out var list))
                {
                    list = new List<IDomainEvent>();
                    Events[taskId] = list;
                }

                list.AddRange(events);
                return Task.CompletedTask;
            }
        }
    }
}
