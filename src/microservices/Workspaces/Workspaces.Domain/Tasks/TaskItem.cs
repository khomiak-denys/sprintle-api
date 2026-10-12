using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Represents a work item or task within a workspace.
    /// Inherits auditing fields (Id, CreatedAt, UpdatedAt) from <see cref="EntityBase"/>.
    /// </summary>
    public class TaskItem : EntityBase
    {
        /// <summary>Gets the unique identifier of the workspace this task belongs to.</summary>
        public Guid WorkspaceId { get; private set; }

        /// <summary>Gets the title of the task.</summary>
        public string Title { get; private set; } = null!;

        /// <summary>Gets the optional description of the task.</summary>
        public string? Description { get; private set; }

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

        private TaskItem() { }

        private TaskItem(
            Guid workspaceId,
            string title,
            string? description,
            Priority priority,
            DateTime? deadline,
            Guid? assigneeId,
            Guid createdById)
        {
            WorkspaceId = workspaceId;
            Title = title;
            Description = description;
            Priority = priority;
            Deadline = deadline;
            AssigneeId = assigneeId;
            CreatedById = createdById;
            Status = TaskStatus.Todo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="TaskItem"/> bound to a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <param name="title">Non-empty task title.</param>
        /// <param name="description">Optional task description.</param>
        /// <param name="priority">Task priority level.</param>
        /// <param name="deadline">Optional deadline.</param>
        /// <param name="assigneeId">Optional assignee user identifier.</param>
        /// <param name="createdById">The creator user identifier.</param>
        /// <returns>A new <see cref="TaskItem"/> instance in 'Todo' status.</returns>
        public static TaskItem Create(
            Guid workspaceId,
            string title,
            string? description,
            Priority priority,
            DateTime? deadline,
            Guid? assigneeId,
            Guid createdById)
        {
            var task = new TaskItem(workspaceId, title, description, priority, deadline, assigneeId, createdById);
            task.OnCreate();
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
        /// </summary>
        /// <param name="assigneeId">The user identifier to assign.</param>
        /// <returns>Success or validation failure result.</returns>
        public Result Assign(Guid assigneeId)
        {
            if (Status != TaskStatus.Todo)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be assigned in 'Todo' status. Current status: '{Status}'."));
            }

            AssigneeId = assigneeId;
            Status = TaskStatus.InProgress;
            OnModify();
            return Result.Success();
        }

        /// <summary>
        /// Transitions the workflow status of the task.
        /// </summary>
        /// <param name="newStatus">Target status.</param>
        /// <returns>Success or validation failure result.</returns>
        public Result ChangeStatus(TaskStatus newStatus)
        {
            var allowed = Status switch
            {
                TaskStatus.Todo => newStatus == TaskStatus.InProgress,
                TaskStatus.InProgress => newStatus == TaskStatus.InReview,
                TaskStatus.InReview => false,
                _ => false
            };

            if (!allowed)
            {
                return Result.Failure(new InvalidArgumentError($"Cannot transition task from '{Status}' to '{newStatus}'."));
            }

            Status = newStatus;
            OnModify();
            return Result.Success();
        }

        /// <summary>
        /// Submits the task for review, moving it from 'InProgress' to 'InReview'.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result SubmitForReview()
        {
            if (Status != TaskStatus.InProgress)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be submitted for review from 'InProgress' status. Current status: '{Status}'."));
            }

            Status = TaskStatus.InReview;
            OnModify();
            return Result.Success();
        }

        /// <summary>
        /// Marks the task as approved and completed ('Done').
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Approve()
        {
            if (Status != TaskStatus.InReview)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be approved from 'InReview' status. Current status: '{Status}'."));
            }

            Status = TaskStatus.Done;
            OnModify();
            return Result.Success();
        }

        /// <summary>
        /// Marks the task as completed ('Done'). Delegates to <see cref="Approve"/>.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Complete() => Approve();

        /// <summary>
        /// Closes the task, moving it from 'Done' to 'Closed'.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Close()
        {
            if (Status != TaskStatus.Done)
            {
                return Result.Failure(new InvalidArgumentError($"Task can only be closed from 'Done' status. Current status: '{Status}'."));
            }

            Status = TaskStatus.Closed;
            OnModify();
            return Result.Success();
        }

        /// <summary>
        /// Logs time spent on the task.
        /// </summary>
        /// <param name="userId">The logging user identifier.</param>
        /// <param name="minutesSpent">Number of minutes spent.</param>
        /// <param name="description">Optional description of work done.</param>
        /// <param name="loggedDate">Date when the work was done.</param>
        public void LogTime(Guid userId, int minutesSpent, string? description, DateOnly loggedDate)
        {
            var log = TimeLog.Create(Id, userId, minutesSpent, description, loggedDate);
            TimeLogs.Add(log);
            OnModify();
        }

        /// <summary>
        /// Cancels the task from 'Todo', 'InProgress', or 'InReview' status.
        /// </summary>
        /// <returns>Success or validation failure result.</returns>
        public Result Cancel()
        {
            if (Status != TaskStatus.Todo && Status != TaskStatus.InProgress && Status != TaskStatus.InReview)
            {
                return Result.Failure(new InvalidArgumentError($"Task cannot be cancelled from '{Status}' status."));
            }

            Status = TaskStatus.Cancelled;
            OnModify();
            return Result.Success();
        }
    }
}
