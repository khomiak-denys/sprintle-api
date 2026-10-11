using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.DTOs
{
    /// <summary>
    /// Represents a task summary item for list projections.
    /// </summary>
    /// <param name="Id">The unique identifier of the task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="Title">The title of the task.</param>
    /// <param name="Description">The optional description of the task.</param>
    /// <param name="Status">The current workflow status of the task.</param>
    /// <param name="Priority">The priority level of the task.</param>
    /// <param name="Deadline">The optional deadline date and time.</param>
    /// <param name="Tags">The list of tag names associated with the task.</param>
    /// <param name="Assignee">The assigned user details, if any.</param>
    public record TaskResult(
        Guid Id,
        Guid WorkspaceId,
        string Title,
        string? Description,
        TaskStatus Status,
        Priority Priority,
        DateTime? Deadline,
        IReadOnlyList<string> Tags,
        UserResult? Assignee = null);
}
