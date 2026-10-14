using System;
using System.Collections.Generic;
using System.Linq;
using DomainFramework;
using FluentAssertions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Tasks.Events;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Domain.Tests.Tasks
{
    public class TaskItemEventSourcingTests
    {
        [Fact]
        public void Create_Should_EmitTaskCreatedDomainEvent_When_TaskIsCreated()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();

            // Act
            var task = TaskItem.Create(workspaceId, "Test Title", "Test Desc", Priority.High, null, null, creatorId);

            // Assert
            task.Status.Should().Be(TaskStatus.Todo);
            task.Version.Should().Be(1);
            task.UncommittedEvents.Should().HaveCount(1);

            var createdEvent = task.UncommittedEvents.First().Should().BeOfType<TaskCreatedDomainEvent>().Subject;
            createdEvent.TaskId.Should().Be(task.Id);
            createdEvent.WorkspaceId.Should().Be(workspaceId);
            createdEvent.Title.Should().Be("Test Title");
            createdEvent.Description.Should().Be("Test Desc");
            createdEvent.Priority.Should().Be(Priority.High);
            createdEvent.CreatedById.Should().Be(creatorId);
            createdEvent.InitialStatus.Should().Be(TaskStatus.Todo);
        }

        [Fact]
        public void Assign_Should_EmitTaskAssignedDomainEvent_When_Valid()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, null, Guid.NewGuid());
            task.ClearUncommittedEvents();
            var assigneeId = Guid.NewGuid();

            // Act
            var result = task.Assign(assigneeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.InProgress);
            task.AssigneeId.Should().Be(assigneeId);
            task.UncommittedEvents.Should().HaveCount(1);

            var assignedEvent = task.UncommittedEvents.First().Should().BeOfType<TaskAssignedDomainEvent>().Subject;
            assignedEvent.TaskId.Should().Be(task.Id);
            assignedEvent.AssigneeId.Should().Be(assigneeId);
            assignedEvent.PreviousStatus.Should().Be(TaskStatus.Todo);
            assignedEvent.NewStatus.Should().Be(TaskStatus.InProgress);
        }

        [Fact]
        public void SubmitForReview_Should_EmitTaskSubmittedForReviewDomainEvent_When_Valid()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, Guid.NewGuid(), Guid.NewGuid());
            task.ClearUncommittedEvents();

            // Act
            var result = task.SubmitForReview();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.InReview);
            task.UncommittedEvents.Should().HaveCount(1);

            var eventItem = task.UncommittedEvents.First().Should().BeOfType<TaskSubmittedForReviewDomainEvent>().Subject;
            eventItem.PreviousStatus.Should().Be(TaskStatus.InProgress);
            eventItem.NewStatus.Should().Be(TaskStatus.InReview);
        }

        [Fact]
        public void Approve_Should_EmitTaskApprovedDomainEvent_When_Valid()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, Guid.NewGuid(), Guid.NewGuid());
            task.SubmitForReview();
            task.ClearUncommittedEvents();

            // Act
            var result = task.Approve();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Done);
            task.UncommittedEvents.Should().HaveCount(1);

            var eventItem = task.UncommittedEvents.First().Should().BeOfType<TaskApprovedDomainEvent>().Subject;
            eventItem.PreviousStatus.Should().Be(TaskStatus.InReview);
            eventItem.NewStatus.Should().Be(TaskStatus.Done);
        }

        [Fact]
        public void Close_Should_EmitTaskClosedDomainEvent_When_Valid()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, Guid.NewGuid(), Guid.NewGuid());
            task.SubmitForReview();
            task.Approve();
            task.ClearUncommittedEvents();

            // Act
            var result = task.Close();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Closed);
            task.UncommittedEvents.Should().HaveCount(1);

            var eventItem = task.UncommittedEvents.First().Should().BeOfType<TaskClosedDomainEvent>().Subject;
            eventItem.PreviousStatus.Should().Be(TaskStatus.Done);
            eventItem.NewStatus.Should().Be(TaskStatus.Closed);
        }

        [Fact]
        public void Cancel_Should_EmitTaskCancelledDomainEvent_When_Valid()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, Guid.NewGuid(), Guid.NewGuid());
            task.ClearUncommittedEvents();

            // Act
            var result = task.Cancel();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Cancelled);
            task.UncommittedEvents.Should().HaveCount(1);

            var eventItem = task.UncommittedEvents.First().Should().BeOfType<TaskCancelledDomainEvent>().Subject;
            eventItem.PreviousStatus.Should().Be(TaskStatus.InProgress);
            eventItem.NewStatus.Should().Be(TaskStatus.Cancelled);
        }

        [Fact]
        public void Rehydrate_Should_ReconstructFullStateAndStatus_When_ReplayingLifecycleEvents()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var events = new List<IDomainEvent>
            {
                new TaskCreatedDomainEvent(taskId, workspaceId, "Rehydrated Task", "Description", Priority.High, null, null, creatorId, TaskStatus.Todo),
                new TaskAssignedDomainEvent(taskId, assigneeId, TaskStatus.Todo, TaskStatus.InProgress),
                new TaskSubmittedForReviewDomainEvent(taskId, TaskStatus.InProgress, TaskStatus.InReview),
                new TaskApprovedDomainEvent(taskId, TaskStatus.InReview, TaskStatus.Done),
                new TaskClosedDomainEvent(taskId, TaskStatus.Done, TaskStatus.Closed)
            };

            // Act
            var task = TaskItem.Rehydrate(taskId, events);

            // Assert
            task.Id.Should().Be(taskId);
            task.WorkspaceId.Should().Be(workspaceId);
            task.Title.Should().Be("Rehydrated Task");
            task.Description.Should().Be("Description");
            task.Priority.Should().Be(Priority.High);
            task.CreatedById.Should().Be(creatorId);
            task.AssigneeId.Should().Be(assigneeId);
            task.Status.Should().Be(TaskStatus.Closed);
            task.Version.Should().Be(5);
            task.UncommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void Rehydrate_Should_SetStatusToCancelled_When_StreamContainsCancellation()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var events = new List<IDomainEvent>
            {
                new TaskCreatedDomainEvent(taskId, workspaceId, "Cancelled Task", null, Priority.Low, null, null, creatorId, TaskStatus.Todo),
                new TaskAssignedDomainEvent(taskId, assigneeId, TaskStatus.Todo, TaskStatus.InProgress),
                new TaskCancelledDomainEvent(taskId, TaskStatus.InProgress, TaskStatus.Cancelled)
            };

            // Act
            var task = TaskItem.Rehydrate(taskId, events);

            // Assert
            task.Status.Should().Be(TaskStatus.Cancelled);
            task.AssigneeId.Should().Be(assigneeId);
            task.Version.Should().Be(3);
        }

        [Fact]
        public void ClearUncommittedEvents_Should_EmptyEventsList_When_Called()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "Title", null, Priority.Medium, null, null, Guid.NewGuid());
            task.UncommittedEvents.Should().NotBeEmpty();

            // Act
            task.ClearUncommittedEvents();

            // Assert
            task.UncommittedEvents.Should().BeEmpty();
        }
    }
}
