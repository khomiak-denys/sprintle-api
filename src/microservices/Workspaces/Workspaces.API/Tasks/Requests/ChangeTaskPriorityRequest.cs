using Workspaces.Application.Tasks.ChangePriority;
using Workspaces.Domain.Tasks;

namespace Workspaces.API.Tasks.Requests
{
    /// <summary>
    /// Represents a request to change the priority level of an existing task.
    /// </summary>
    /// <param name="Priority">The new priority level to assign to the task.</param>
    public record ChangeTaskPriorityRequest(Priority Priority)
    {
        /// <summary>
        /// Converts the request to a <see cref="ChangeTaskPriorityCommand"/>.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="requestedById">The unique identifier of the user requesting the change.</param>
        /// <param name="isAdmin">Whether the user has administrator privileges.</param>
        /// <returns>A new <see cref="ChangeTaskPriorityCommand"/> instance.</returns>
        public ChangeTaskPriorityCommand ToCommand(Guid taskId, Guid requestedById, bool isAdmin) =>
            new(taskId, Priority, requestedById, isAdmin);
    }
}
