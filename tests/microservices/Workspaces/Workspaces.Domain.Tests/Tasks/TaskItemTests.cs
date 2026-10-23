using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Tasks.Events;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Domain.Tests.Tasks
{
    public class TaskItemTests
    {
        [Fact]
        public void Create_Should_SetStatusToInProgress_When_AssigneeIdIsProvided()
        {
            // Arrange
            var assigneeId = Guid.NewGuid();

            // Act
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task with Assignee", null, Priority.High, null, assigneeId, Guid.NewGuid());

            // Assert
            task.Status.Should().Be(TaskStatus.InProgress);
            task.AssigneeId.Should().Be(assigneeId);
            task.GetAvailableTransitions().Should().Equal(TaskAction.SubmitForReview, TaskAction.Cancel);
        }

        [Fact]
        public void Create_Should_SetStatusToTodo_When_AssigneeIdIsNull()
        {
            // Act
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Unassigned Task", null, Priority.Low, null, null, Guid.NewGuid());

            // Assert
            task.Status.Should().Be(TaskStatus.Todo);
            task.AssigneeId.Should().BeNull();
            task.GetAvailableTransitions().Should().Equal(TaskAction.Assign, TaskAction.Cancel);
        }

        [Fact]
        public void GetAvailableTransitions_Should_ReturnAssignAndCancel_When_StatusIsTodo()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());

            // Act
            var transitions = task.GetAvailableTransitions();

            // Assert
            transitions.Should().Equal(TaskAction.Assign, TaskAction.Cancel);
        }

        [Fact]
        public void Assign_Should_MoveToInProgress_When_StatusIsTodo()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var assigneeId = Guid.NewGuid();

            // Act
            var result = task.Assign(assigneeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.InProgress);
            task.AssigneeId.Should().Be(assigneeId);
            task.GetAvailableTransitions().Should().Equal(TaskAction.SubmitForReview, TaskAction.Cancel);
        }

        [Fact]
        public void Assign_Should_Fail_When_StatusIsNotTodo()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            task.Assign(Guid.NewGuid()); // now InProgress

            // Act
            var result = task.Assign(Guid.NewGuid());

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
        }

        [Fact]
        public void SubmitForReview_Should_TransitionToInReview_When_StatusIsInProgress()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            task.Assign(Guid.NewGuid());

            // Act
            var result = task.SubmitForReview();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.InReview);
            task.GetAvailableTransitions().Should().Equal(TaskAction.Approve, TaskAction.Cancel);
        }

        [Fact]
        public void SubmitForReview_Should_Fail_When_StatusIsNotInProgress()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());

            // Act
            var result = task.SubmitForReview();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
        }

        [Fact]
        public void Approve_Should_TransitionToDone_When_StatusIsInReview()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            task.Assign(Guid.NewGuid());
            task.SubmitForReview();

            // Act
            var result = task.Approve();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Done);
            task.GetAvailableTransitions().Should().Equal(TaskAction.Close);
        }

        [Fact]
        public void Approve_Should_Fail_When_StatusIsNotInReview()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());

            // Act
            var result = task.Approve();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
        }

        [Fact]
        public void Close_Should_TransitionToClosed_When_StatusIsDone()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            task.Assign(Guid.NewGuid());
            task.SubmitForReview();
            task.Approve();

            // Act
            var result = task.Close();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Closed);
            task.GetAvailableTransitions().Should().BeEmpty();
        }

        [Fact]
        public void Close_Should_Fail_When_StatusIsNotDone()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());

            // Act
            var result = task.Close();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
        }

        [Theory]
        [InlineData(TaskStatus.Todo)]
        [InlineData(TaskStatus.InProgress)]
        [InlineData(TaskStatus.InReview)]
        public void Cancel_Should_Succeed_When_StatusIsCancellable(TaskStatus initialStatus)
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            if (initialStatus == TaskStatus.InProgress)
            {
                task.Assign(Guid.NewGuid());
            }
            else if (initialStatus == TaskStatus.InReview)
            {
                task.Assign(Guid.NewGuid());
                task.SubmitForReview();
            }

            // Act
            var result = task.Cancel();

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Cancelled);
            task.GetAvailableTransitions().Should().BeEmpty();
        }

        [Fact]
        public void Cancel_Should_Fail_When_StatusIsDoneOrClosed()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            task.Assign(Guid.NewGuid());
            task.SubmitForReview();
            task.Approve();

            // Act
            var result = task.Cancel();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidArgumentError>();
        }

        [Fact]
        public void ChangePriority_Should_UpdatePriority_When_Called()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Low, null, null, Guid.NewGuid());

            // Act
            task.ChangePriority(Priority.Critical);

            // Assert
            task.Priority.Should().Be(Priority.Critical);
            task.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Create_Should_SetNumber_When_ValidNumberProvided()
        {
            // Act
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-42", "Task 42", null, Priority.High, null, null, Guid.NewGuid());

            // Assert
            task.Number.Should().Be("DMS-42");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_Should_ThrowArgumentException_When_NumberIsNullOrWhiteSpace(string? invalidNumber)
        {
            // Act
            var act = () => TaskItem.Create(Guid.NewGuid(), invalidNumber!, "Invalid Task", null, Priority.Medium, null, null, Guid.NewGuid());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RestoreNumber_Should_SetNumber_When_CurrentNumberIsEmpty()
        {
            // Arrange - simulate legacy rehydrated task with Number = ""
            var task = TaskItem.Rehydrate(Guid.NewGuid(), new[]
            {
                new TaskCreatedDomainEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Legacy Task",
                    null,
                    Priority.Low,
                    null,
                    null,
                    Guid.NewGuid(),
                    TaskStatus.Todo,
                    "")
            });
            task.Number.Should().BeEmpty();

            // Act
            task.RestoreNumber("DMS-15");

            // Assert
            task.Number.Should().Be("DMS-15");
        }

        [Fact]
        public void RestoreNumber_Should_NotChangeNumber_When_AlreadySet()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-10", "Task", null, Priority.Low, null, null, Guid.NewGuid());

            // Act
            task.RestoreNumber("DMS-99");

            // Assert
            task.Number.Should().Be("DMS-10");
        }
    }
}
