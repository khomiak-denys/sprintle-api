using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a task workflow status has changed.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="TaskTitle">The title of the task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="WorkspaceName">The name of the workspace.</param>
    /// <param name="OldStatus">The previous status of the task before the transition.</param>
    /// <param name="NewStatus">The new status of the task after the transition.</param>
    /// <param name="ChangedByUserId">The unique identifier of the user who changed the status.</param>
    /// <param name="ChangedByUserName">The username or display name of the user who changed the status.</param>
    /// <param name="CreatedByUserId">The unique identifier of the user who created the task (the requester).</param>
    /// <param name="CreatedByEmail">The email address of the task requester.</param>
    /// <param name="CreatedByName">The display name or username of the task requester.</param>
    /// <param name="AssigneeUserId">The optional unique identifier of the assigned user.</param>
    /// <param name="AssigneeEmail">The optional email address of the assigned user.</param>
    /// <param name="AssigneeName">The optional display name or username of the assigned user.</param>
    /// <param name="TaskNumber">The sequential task number within its workspace. Optional for backward compatibility with messages published before task numbering existed.</param>
    public sealed record TaskStatusChanged(
        Guid TaskId,
        string TaskTitle,
        Guid WorkspaceId,
        string WorkspaceName,
        string OldStatus,
        string NewStatus,
        Guid ChangedByUserId,
        string ChangedByUserName,
        Guid CreatedByUserId,
        string CreatedByEmail,
        string CreatedByName,
        Guid? AssigneeUserId,
        string? AssigneeEmail,
        string? AssigneeName,
        string? TaskNumber = null);
}
