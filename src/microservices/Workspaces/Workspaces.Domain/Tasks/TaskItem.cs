using System;
using System.Collections.Generic;
using System.Linq;
using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Workspaces.Domain.Tasks.Events;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Represents a work item or task aggregate root within a workspace.
    /// Supports event sourcing for status transitions and lifecycle events.
    /// </summary>
    public class TaskItem : AggregateRoot
    {
        /// <summary>Gets the unique identifier of the workspace this task belongs to.</summary>
        public Guid WorkspaceId { get; private set; }

        /// <summary>
        /// Gets the human-readable identifier of the task, formatted as {WorkspacePrefix}-{Number} (e.g. DMS-730).
        /// Allocated by the backend at creation time and never changed afterwards.
        /// </summary>
        public string Number { get; private set; } = null!;

        /// <summary>Gets the title of the task.</summary>
        public string Title { get; private set; } = null!;

        /// <summary>Gets the optional description of the task.</summary>
        public string? Description { get; private set; }

        /// <summary>Gets the type of the task.</summary>
        public TaskType Type { get; private set; }

        /// <summary>Gets the current workflow status of the task.</summary>
        public TaskStatus Status { get; private set; }

        /// <summary>Gets the priority level of the task.</summary>
        public Priority Priority { get; private set; }

        /// <summary>Gets the optional deadline date and time for the task.</summary>
        public DateTime? Deadline { get; private set; }

        /// <summary>Gets the unique identifier of the user assigned to this task, if any.</summary>
        public Guid? AssigneeId { get; private set; }

        /// <summary>Gets the unique identifier of the user who created this task.</summary>
        public Guid CreatedById { get; private set; }

        /// <summary>Gets the collection of time logs recorded against this task.</summary>
        public ICollection<TimeLog> TimeLogs { get; private set; } = new List<TimeLog>();

        /// <summary>Gets the collection of tag join entities associated with this task.</summary>
        public ICollection<TaskTag> TaskTags { get; private set; } = new List<TaskTag>();

        /// <summary>Gets the collection of comments posted on this task.</summary>
        public ICollection<TaskComment> Comments { get; private set; } = new List<TaskComment>();

        private TaskItem() { }

        private TaskItem(Guid id) : base(id) { }

        private TaskItem(
            Guid workspaceId,
            string title,
            string? description,
            TaskType type,
            Priority priority,
            DateTime? deadline,
            Guid? assigneeId,
            Guid createdById)
        {
            WorkspaceId = workspaceId;
            Title = title;
            Description = description;
            Type = type;
            Priority = priority;
            Deadline = deadline;
            AssigneeId = assigneeId;
            CreatedById = createdById;
            Status = assigneeId.HasValue ? TaskStatus.InProgress : TaskStatus.Todo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="TaskItem"/> bound to a workspace.
        /// Raises a <see cref="TaskCreatedDomainEvent"/>.
        /// </summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <param name="number">The task identifier (e.g. DMS-730), unique within its workspace.</param>
        /// <param name="title">Non-empty task title.</param>
        /// <param name="description">Optional task description.</param>
        /// <param name="priority">Task priority level.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="assigneeId">Optional assignee user identifier. When provided, the task transitions to 'InProgress'; otherwise 'Todo'.</param>
        /// <param name="createdById">The creator user identifier.</param>
        /// <returns>A new <see cref="TaskItem"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="number"/> is null or whitespace.</exception>
        public static TaskItem Create(
            Guid workspaceId,
            string number,
            string title,
            string? description,
            TaskType type,
            Priority priority,
            DateTime? deadline,
            Guid? assigneeId,
            Guid createdById)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(number);

            var task = new TaskItem(Guid.NewGuid());
            var initialStatus = assigneeId.HasValue ? TaskStatus.InProgress : TaskStatus.Todo;
            task.RaiseEvent(new TaskCreatedDomainEvent(
                task.Id,
                workspaceId,
                title,
                description,
                type,
                priority,
                deadline,
                assigneeId,
                createdById,
                initialStatus,
                number));
            return task;
        }


        /// <summary>
        /// Rehydrates a <see cref="TaskItem"/> aggregate root from an ordered stream of historical domain events.
        /// </summary>
        /// <param name="id">The unique identifier of the task.</param>
        /// <param name="events">The ordered collection of domain events.</param>
        /// <returns>A rehydrated <see cref="TaskItem"/> with state matching the event stream.</returns>
        public static TaskItem Rehydrate(Guid id, IEnumerable<IDomainEvent> events)
        {
            var task = new TaskItem(id);
            task.Rehydrate(events);
            return task;
        }

        /// <summary>
        /// Moves the task to a different workspace.
        /// </summary>
        /// <param name="workspaceId">The target workspace identifier.</param>
        public void MoveToWorkspace(Guid workspaceId)
        {
            WorkspaceId = workspaceId;
            OnModify();
        }

        /// <summary>
        /// Updates mutable task fields.
        /// </summary>
        /// <param name="title">New title.</param>
        /// <param name="description">New description.</param>
        /// <param name="priority">New priority.</param>
        /// <param name="deadline">New deadline.</param>
        public void Update(string title, string? description, Priority priority, DateTime? deadline)
        {
            Title = title;
            Description = description;
            Priority = priority;
            Deadline = deadline;
            OnModify();
        }

        /// <summary>
        /// Updates the priority of the task.
        /// </summary>
        /// <param name="priority">The new priority level.</param>
        public void ChangePriority(Priority priority)
        {
            Priority = priority;
            OnModify();
        }

        /// <summary>
        /// Returns the list of workflow actions that are structurally valid
        /// from the current <see cref="Status"/>.
        /// Authorization checks (role, assignee) are evaluated at the application layer.
        /// </summary>
        /// <returns>A read-only collection of available <see cref="TaskAction"/> values.</returns>
        public IReadOnlyList<TaskAction> GetAvailableTransitions()
        {
            return Status switch
            {
                TaskStatus.Todo => new[] { TaskAction.Assign, TaskAction.Cancel },
                TaskStatus.InProgress => new[] { TaskAction.SubmitForReview, TaskAction.Cancel },
                TaskStatus.InReview => new[] { TaskAction.Approve, TaskAction.Cancel },
                TaskStatus.Done => new[] { TaskAction.Close },
                _ => Array.Empty<TaskAction>()
            };
        }

        /// <summary>
        /// Assigns the task to a user and transitions its status from 'Todo' to 'InProgress'.
        /// Raises a <see cref="TaskAssignedDomainEvent"/>.
        /// </summary>
        /// <param name="assigneeId">The user identifier to assign.</param>
        /// <returns>Success or validation failure result.</returns>
        public Result Assign(Guid assigneeId)
        {
            if (Status != TaskStatus.Todo)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be assigned in 'Todo' status. Current status: '{Status}'."));
            }

            RaiseEvent(new TaskAssignedDomainEvent(Id, assigneeId, Status, TaskStatus.InProgress));
            return Result.Success();
        }

        /// <summary>
        /// Submits the task for review, moving it from 'InProgress' to 'InReview'.
        /// Raises a <see cref="TaskSubmittedForReviewDomainEvent"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result SubmitForReview()
        {
            if (Status != TaskStatus.InProgress)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be submitted for review from 'InProgress' status. Current status: '{Status}'."));
            }

            RaiseEvent(new TaskSubmittedForReviewDomainEvent(Id, Status, TaskStatus.InReview));
            return Result.Success();
        }

        /// <summary>
        /// Marks the task as approved and completed ('Done').
        /// Raises a <see cref="TaskApprovedDomainEvent"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Approve()
        {
            if (Status != TaskStatus.InReview)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be approved from 'InReview' status. Current status: '{Status}'."));
            }

            RaiseEvent(new TaskApprovedDomainEvent(Id, Status, TaskStatus.Done));
            return Result.Success();
        }

        /// <summary>
        /// Marks the task as completed ('Done'). Delegates to <see cref="Approve"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Complete() => Approve();

        /// <summary>
        /// Closes the task, moving it from 'Done' to 'Closed'.
        /// Raises a <see cref="TaskClosedDomainEvent"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Close()
        {
            if (Status != TaskStatus.Done)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be closed from 'Done' status. Current status: '{Status}'."));
            }

            RaiseEvent(new TaskClosedDomainEvent(Id, Status, TaskStatus.Closed));
            return Result.Success();
        }

        /// <summary>
        /// Cancels the task from 'Todo', 'InProgress', or 'InReview' status.
        /// Raises a <see cref="TaskCancelledDomainEvent"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Cancel()
        {
            if (Status != TaskStatus.Todo && Status != TaskStatus.InProgress && Status != TaskStatus.InReview)
            {
                return Result.Failure(new InvalidArgumentError($"Task cannot be cancelled from '{Status}' status."));
            }

            RaiseEvent(new TaskCancelledDomainEvent(Id, Status, TaskStatus.Cancelled));
            return Result.Success();
        }

        /// <summary>
        /// Logs time spent on the task.
        /// </summary>
        /// <param name="userId">The logging user identifier.</param>
        /// <param name="minutesSpent">Number of minutes spent.</param>
        /// <param name="description">Optional description of work done.</param>
        /// <param name="loggedDate">Date when the work was done.</param>
        /// <returns>The created <see cref="TimeLog"/>.</returns>
        public TimeLog LogTime(Guid userId, int minutesSpent, string? description, DateOnly loggedDate)
        {
            var log = TimeLog.Create(Id, userId, minutesSpent, description, loggedDate);
            TimeLogs.Add(log);
            OnModify();
            return log;
        }

        /// <summary>
        /// Adds a comment to this task.
        /// </summary>
        /// <param name="authorId">The commenting user identifier.</param>
        /// <param name="content">Non-empty comment text.</param>
        /// <returns>The created <see cref="TaskComment"/>.</returns>
        public TaskComment AddComment(Guid authorId, string content)
        {
            var comment = TaskComment.Create(Id, authorId, content);
            Comments.Add(comment);
            OnModify();
            return comment;
        }

        /// <summary>
        /// Removes a comment from this task if it exists in the tracked collection and the user is authorized.
        /// </summary>
        /// <param name="commentId">The comment identifier.</param>
        /// <param name="requestingUserId">The user requesting deletion.</param>
        /// <param name="isAdmin">Whether the requesting user is an admin.</param>
        /// <returns>Success or failure result.</returns>
        public Result RemoveComment(Guid commentId, Guid requestingUserId, bool isAdmin)
        {
            var comment = Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment is null)
            {
                return Result.Failure(new NotFoundError($"Comment '{commentId}' was not found."));
            }

            if (comment.AuthorId != requestingUserId && !isAdmin)
            {
                return Result.Failure(new ForbiddenError("Only the comment author or an admin can delete a comment."));
            }

            Comments.Remove(comment);
            OnModify();
            return Result.Success();
        }

        /// <inheritdoc/>
        protected override void Apply(IDomainEvent @event)
        {
            switch (@event)
            {
                case TaskCreatedDomainEvent e:
                    WorkspaceId = e.WorkspaceId;
                    Number = e.Number;
                    Title = e.Title;
                    Description = e.Description;
                    Type = e.Type;
                    Priority = e.Priority;
                    Deadline = e.Deadline;
                    AssigneeId = e.AssigneeId;
                    CreatedById = e.CreatedById;
                    Status = e.InitialStatus;
                    CreatedAt = e.OccurredAt;
                    break;

                case TaskAssignedDomainEvent e:
                    AssigneeId = e.AssigneeId;
                    Status = e.NewStatus;
                    UpdatedAt = e.OccurredAt;
                    break;

                case TaskSubmittedForReviewDomainEvent e:
                    Status = e.NewStatus;
                    UpdatedAt = e.OccurredAt;
                    break;

                case TaskApprovedDomainEvent e:
                    Status = e.NewStatus;
                    UpdatedAt = e.OccurredAt;
                    break;

                case TaskClosedDomainEvent e:
                    Status = e.NewStatus;
                    UpdatedAt = e.OccurredAt;
                    break;

                case TaskCancelledDomainEvent e:
                    Status = e.NewStatus;
                    UpdatedAt = e.OccurredAt;
                    break;
            }
        }
    }
}
