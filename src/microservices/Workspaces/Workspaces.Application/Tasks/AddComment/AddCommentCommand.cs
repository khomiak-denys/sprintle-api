using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.AddComment
{
    /// <summary>
    /// Command to add a comment to a task.
    /// </summary>
    /// <param name="TaskId">The task identifier.</param>
    /// <param name="UserId">The commenting user identifier.</param>
    /// <param name="IsAdmin">Whether the user is an admin.</param>
    /// <param name="Content">The comment text.</param>
    public record AddCommentCommand(
        Guid TaskId,
        Guid UserId,
        bool IsAdmin,
        string Content) : ICommand<Result<Guid>>;
}
