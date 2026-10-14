using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when time is logged on a task.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="TaskTitle">The title of the task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="WorkspaceName">The name of the workspace.</param>
    /// <param name="TimeLogId">The unique identifier of the time log entry.</param>
    /// <param name="MinutesSpent">Number of minutes spent.</param>
    /// <param name="Description">Optional note describing the work done.</param>
    /// <param name="LoggedDate">Calendar date when work was performed.</param>
    /// <param name="LoggedByUserId">The unique identifier of the user who logged time.</param>
    /// <param name="LoggedByUserName">The display name or username of the user who logged time.</param>
    /// <param name="CreatedByUserId">The unique identifier of the task requester (creator).</param>
    /// <param name="CreatedByEmail">The email address of the task requester.</param>
    /// <param name="CreatedByName">The display name or username of the task requester.</param>
    public sealed record TaskTimeLogged(
        Guid TaskId,
        string TaskTitle,
        Guid WorkspaceId,
        string WorkspaceName,
        Guid TimeLogId,
        int MinutesSpent,
        string? Description,
        DateOnly LoggedDate,
        Guid LoggedByUserId,
        string LoggedByUserName,
        Guid CreatedByUserId,
        string CreatedByEmail,
        string CreatedByName);
}
