using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a task is assigned to a user.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the assigned task.</param>
    /// <param name="TaskTitle">The title of the assigned task.</param>
    /// <param name="TaskDescription">The optional description of the assigned task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="WorkspaceName">The name of the workspace the task belongs to.</param>
    /// <param name="AssigneeUserId">The unique identifier of the user assigned to the task.</param>
    /// <param name="AssigneeEmail">The email address of the assigned user.</param>
    /// <param name="AssigneeName">The display name or username of the assigned user.</param>
    /// <param name="AssignedByUserId">The unique identifier of the user who assigned the task.</param>
    /// <param name="AssignedByName">The display name or username of the user who assigned the task.</param>
    /// <param name="Priority">The optional priority level name of the task.</param>
    /// <param name="Deadline">The optional deadline date and time for the task.</param>
    /// <param name="TaskNumber">The sequential task number within its workspace. Optional for backward compatibility with messages published before task numbering existed.</param>
    public sealed record TaskAssigned(
        Guid TaskId,
        string TaskTitle,
        string? TaskDescription,
        Guid WorkspaceId,
        string WorkspaceName,
        Guid AssigneeUserId,
        string AssigneeEmail,
        string AssigneeName,
        Guid AssignedByUserId,
        string AssignedByName,
        string? Priority,
        DateTime? Deadline,
        string? TaskNumber = null);
}
