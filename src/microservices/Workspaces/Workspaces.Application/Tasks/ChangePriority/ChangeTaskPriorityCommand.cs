using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.ChangePriority
{
    /// <summary>
    /// Command to change the priority of a task.
    /// Allowed only for administrators or the task creator (requester).
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task whose priority is to be updated.</param>
    /// <param name="Priority">The new priority level for the task.</param>
    /// <param name="RequestedById">The unique identifier of the user requesting the priority change.</param>
    /// <param name="IsAdmin">Whether the requesting user has administrator privileges.</param>
    public record ChangeTaskPriorityCommand(
        Guid TaskId,
        Priority Priority,
        Guid RequestedById,
        bool IsAdmin) : ICommand<Result>;
}
