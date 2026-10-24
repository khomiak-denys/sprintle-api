using System;
using DomainFramework;
using FluentAssertions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Tasks.Events;
using Workspaces.Infrastructure.Persistence.EventSourcing;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Infrastructure.Tests.EventSourcing
{
    public class TaskEventSerializerTests
    {
        [Fact]
        public void SerializeAndDeserialize_Should_PreserveAllProperties_When_TaskCreatedDomainEvent()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var original = new TaskCreatedDomainEvent(
                taskId,
                workspaceId,
                "Serialize Test",
                "Description",
                TaskType.Feature,
                Priority.High,
                DateTime.UtcNow.AddDays(3),
                assigneeId,
                creatorId,
                TaskStatus.InProgress,
                "DMS-42");

            // Act
            var (eventType, payload) = TaskEventSerializer.Serialize(original);
            var deserialized = TaskEventSerializer.Deserialize(eventType, payload) as TaskCreatedDomainEvent;

            // Assert
            eventType.Should().Be(nameof(TaskCreatedDomainEvent));
            deserialized.Should().NotBeNull();
            deserialized!.TaskId.Should().Be(taskId);
            deserialized.WorkspaceId.Should().Be(workspaceId);
            deserialized.Number.Should().Be("DMS-42");
            deserialized.Title.Should().Be("Serialize Test");
            deserialized.Description.Should().Be("Description");
            deserialized.Type.Should().Be(TaskType.Feature);
            deserialized.Priority.Should().Be(Priority.High);
            deserialized.AssigneeId.Should().Be(assigneeId);
            deserialized.CreatedById.Should().Be(creatorId);
            deserialized.InitialStatus.Should().Be(TaskStatus.InProgress);
        }

        [Fact]
        public void Deserialize_Should_DefaultNumberToEmpty_When_LegacyPayloadHasNoNumber()
        {
            // Arrange - JSON payload persisted before task numbering was added
            var taskId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var legacyJson = $$"""
            {
                "TaskId": "{{taskId}}",
                "WorkspaceId": "{{workspaceId}}",
                "Title": "Legacy Task",
                "Description": null,
                "Priority": 1,
                "Deadline": null,
                "AssigneeId": null,
                "CreatedById": "{{creatorId}}",
                "InitialStatus": 1
            }
            """;

            // Act
            var deserialized = TaskEventSerializer.Deserialize(nameof(TaskCreatedDomainEvent), legacyJson) as TaskCreatedDomainEvent;

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Number.Should().BeEmpty();
            deserialized.Title.Should().Be("Legacy Task");
        }

        [Fact]
        public void SerializeAndDeserialize_Should_PreserveAllProperties_When_TaskAssignedDomainEvent()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var original = new TaskAssignedDomainEvent(taskId, assigneeId, TaskStatus.Todo, TaskStatus.InProgress);

            // Act
            var (eventType, payload) = TaskEventSerializer.Serialize(original);
            var deserialized = TaskEventSerializer.Deserialize(eventType, payload) as TaskAssignedDomainEvent;

            // Assert
            eventType.Should().Be(nameof(TaskAssignedDomainEvent));
            deserialized.Should().NotBeNull();
            deserialized!.TaskId.Should().Be(taskId);
            deserialized.AssigneeId.Should().Be(assigneeId);
            deserialized.PreviousStatus.Should().Be(TaskStatus.Todo);
            deserialized.NewStatus.Should().Be(TaskStatus.InProgress);
        }

        [Fact]
        public void SerializeAndDeserialize_Should_PreserveAllProperties_When_TaskCancelledDomainEvent()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var original = new TaskCancelledDomainEvent(taskId, TaskStatus.InProgress, TaskStatus.Cancelled);

            // Act
            var (eventType, payload) = TaskEventSerializer.Serialize(original);
            var deserialized = TaskEventSerializer.Deserialize(eventType, payload) as TaskCancelledDomainEvent;

            // Assert
            eventType.Should().Be(nameof(TaskCancelledDomainEvent));
            deserialized.Should().NotBeNull();
            deserialized!.TaskId.Should().Be(taskId);
            deserialized.PreviousStatus.Should().Be(TaskStatus.InProgress);
            deserialized.NewStatus.Should().Be(TaskStatus.Cancelled);
        }

        [Fact]
        public void Deserialize_Should_ThrowInvalidOperationException_When_EventTypeIsUnknown()
        {
            // Arrange & Act
            var action = () => TaskEventSerializer.Deserialize("UnknownEvent", "{}");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*Unknown event type*");
        }
    }
}
