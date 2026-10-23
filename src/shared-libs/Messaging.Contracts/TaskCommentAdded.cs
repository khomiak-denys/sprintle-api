using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a comment is added to a task.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="TaskTitle">The title of the task.</param>
    /// <param name="WorkspaceId">The unique identifier of the workspace the task belongs to.</param>
    /// <param name="WorkspaceName">The name of the workspace.</param>
    /// <param name="CommentId">The unique identifier of the comment.</param>
    /// <param name="CommentContent">The textual content of the comment.</param>
    /// <param name="CommentAuthorUserId">The unique identifier of the user who posted the comment.</param>
    /// <param name="CommentAuthorName">The display name or username of the user who posted the comment.</param>
    /// <param name="CreatedByUserId">The unique identifier of the task requester (creator).</param>
    /// <param name="CreatedByEmail">The email address of the task requester.</param>
    /// <param name="CreatedByName">The display name or username of the task requester.</param>
    /// <param name="AssigneeUserId">The optional unique identifier of the assigned user.</param>
    /// <param name="AssigneeEmail">The optional email address of the assigned user.</param>
    /// <param name="AssigneeName">The optional display name or username of the assigned user.</param>
    /// <param name="TaskNumber">The sequential task number within its workspace. Optional for backward compatibility with messages published before task numbering existed.</param>
    public sealed record TaskCommentAdded(
        Guid TaskId,
        string TaskTitle,
        Guid WorkspaceId,
        string WorkspaceName,
        Guid CommentId,
        string CommentContent,
        Guid CommentAuthorUserId,
        string CommentAuthorName,
        Guid CreatedByUserId,
        string CreatedByEmail,
        string CreatedByName,
        Guid? AssigneeUserId,
        string? AssigneeEmail,
        string? AssigneeName,
        string? TaskNumber = null);
}
