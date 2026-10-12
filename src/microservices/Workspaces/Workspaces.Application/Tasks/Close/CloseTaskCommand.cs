using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.Close
{
    /// <summary>
    /// Command to close a completed task (Done → Closed).
    /// Allowed only for administrators or the task creator (requester).
    /// </summary>
    /// <param name="TaskId">The task to close.</param>
    /// <param name="RequestedById">Id of the authenticated user closing the task.</param>
    /// <param name="IsAdmin">Whether the requesting user has administrator privileges.</param>
    public record CloseTaskCommand(
        Guid TaskId,
        Guid RequestedById,
        bool IsAdmin) : ICommand<Result>;
}
