using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.Cancel
{
    /// <summary>
    /// Command to cancel a task.
    /// Allowed only for administrators or the task creator (requester) from Todo, InProgress, or InReview.
    /// </summary>
    /// <param name="TaskId">The task to cancel.</param>
    /// <param name="RequestedById">Id of the authenticated user requesting the cancellation.</param>
    /// <param name="IsAdmin">Whether the requesting user has administrator privileges.</param>
    public record CancelTaskCommand(
        Guid TaskId,
        Guid RequestedById,
        bool IsAdmin) : ICommand<Result>;
}
