using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.DTOs
{
    /// <summary>
    /// Describes an available status transition with its workflow action, target status, and authorization state.
    /// </summary>
    /// <param name="Action">The <see cref="TaskAction"/> workflow action.</param>
    /// <param name="TargetStatus">The target <see cref="TaskStatus"/> of the transition.</param>
    /// <param name="IsAllowed">Whether the requesting user is authorized to perform this transition.</param>
    public record TaskTransitionResult(
        TaskAction Action,
        TaskStatus TargetStatus,
        bool IsAllowed);
}
