using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.DTOs
{
    /// <summary>
    /// Represents the full details of a task, including time logs, assignees, and available workflow transitions.
    /// </summary>
    /// <param name="Id">The unique identifier of the task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="Title">The title of the task.</param>
    /// <param name="Description">The optional description of the task.</param>
    /// <param name="Status">The current workflow status of the task.</param>
    /// <param name="Priority">The priority level of the task.</param>
    /// <param name="Deadline">The optional deadline date and time.</param>
    /// <param name="Assignee">The assigned user details, if any.</param>
    /// <param name="CreatedBy">The user who created the task.</param>
    /// <param name="CreatedAt">The timestamp when the task was created.</param>
    /// <param name="UpdatedAt">The optional timestamp when the task was last updated.</param>
    /// <param name="Tags">The list of tag names associated with the task.</param>
    /// <param name="TimeLogs">The list of time logs logged against the task.</param>
    /// <param name="AvailableTransitions">The list of available status transitions for this task.</param>
    public record TaskDetailsResult(
        Guid Id,
        Guid WorkspaceId,
        string Title,
        string? Description,
        TaskStatus Status,
        Priority Priority,
        DateTime? Deadline,
        UserResult? Assignee,
        UserResult CreatedBy,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        IReadOnlyList<string> Tags,
        IReadOnlyList<TimeLogResult> TimeLogs,
        IReadOnlyList<TaskTransitionResult>? AvailableTransitions = null)
    {
        /// <summary>
        /// Gets the assigned user details.
        /// </summary>
        public UserResult? AssignedUser => Assignee;

        /// <summary>
        /// Gets the creating user details.
        /// </summary>
        public UserResult CreatedByUser => CreatedBy;

        /// <summary>
        /// Gets the available transitions or an empty list if null.
        /// </summary>
        public IReadOnlyList<TaskTransitionResult> Transitions => AvailableTransitions ?? Array.Empty<TaskTransitionResult>();
    }
}
