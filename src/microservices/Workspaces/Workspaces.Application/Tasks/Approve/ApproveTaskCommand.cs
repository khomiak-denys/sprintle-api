using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.Approve
{
    /// <summary>
    /// Command to approve a task (InReview → Done).
    /// Allowed only for administrators or the task creator (requester).
    /// </summary>
    /// <param name="TaskId">The task to approve.</param>
    /// <param name="RequestedById">Id of the authenticated user approving the task.</param>
    /// <param name="IsAdmin">Whether the requesting user has administrator privileges.</param>
    public record ApproveTaskCommand(
        Guid TaskId,
        Guid RequestedById,
        bool IsAdmin) : ICommand<Result>;
}
