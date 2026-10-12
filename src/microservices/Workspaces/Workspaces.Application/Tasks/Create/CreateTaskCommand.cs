using DomainFramework.Results;
using Messaging.Abstractions;
using Priority = Workspaces.Domain.Tasks.Priority;

namespace Workspaces.Application.Tasks.Create
{
    /// <summary>
    /// Command to create a new task bound to a workspace.
    /// </summary>
    /// <param name="WorkspaceId">Id of the workspace the task belongs to.</param>
    /// <param name="CreatedById">Id of the authenticated user creating the task.</param>
    /// <param name="Title">Non-empty title (max 200 chars).</param>
    /// <param name="Description">Optional description.</param>
    /// <param name="Priority">Initial priority level.</param>
    /// <param name="Deadline">Optional future deadline.</param>
    /// <param name="AssigneeId">Optional user id to assign the task to.</param>
    /// <param name="TagNames">Set of tag names to associate with the task.</param>
    /// <param name="IsAdmin">Whether the requesting user has administrator privileges.</param>
    public record CreateTaskCommand(
        Guid WorkspaceId,
        Guid CreatedById,
        string Title,
        string? Description,
        Priority Priority,
        DateTime? Deadline,
        Guid? AssigneeId,
        IEnumerable<string> TagNames,
        bool IsAdmin = false) : ICommand<Result<Guid>>;
}
