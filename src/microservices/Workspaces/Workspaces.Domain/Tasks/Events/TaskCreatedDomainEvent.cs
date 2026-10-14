using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a new task is created within a workspace.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="WorkspaceId">The workspace identifier.</param>
    /// <param name="Title">The task title.</param>
    /// <param name="Description">Optional task description.</param>
    /// <param name="Priority">The task priority level.</param>
    /// <param name="Deadline">Optional task deadline.</param>
    /// <param name="AssigneeId">Optional assigned user identifier.</param>
    /// <param name="CreatedById">The creator user identifier.</param>
    /// <param name="InitialStatus">The initial status of the task.</param>
    public sealed record TaskCreatedDomainEvent(
        Guid TaskId,
        Guid WorkspaceId,
        string Title,
        string? Description,
        Priority Priority,
        DateTime? Deadline,
        Guid? AssigneeId,
        Guid CreatedById,
        TaskStatus InitialStatus) : DomainEventBase(TaskId);
}
