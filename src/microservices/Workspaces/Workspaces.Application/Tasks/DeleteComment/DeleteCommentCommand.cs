using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.DeleteComment
{
    /// <summary>
    /// Command to delete a comment from a task.
    /// </summary>
    /// <param name="TaskId">The task identifier.</param>
    /// <param name="CommentId">The comment identifier.</param>
    /// <param name="UserId">The requesting user identifier.</param>
    /// <param name="IsAdmin">Whether the requesting user is an admin.</param>
    public record DeleteCommentCommand(
        Guid TaskId,
        Guid CommentId,
        Guid UserId,
        bool IsAdmin) : ICommand<Result>;
}
