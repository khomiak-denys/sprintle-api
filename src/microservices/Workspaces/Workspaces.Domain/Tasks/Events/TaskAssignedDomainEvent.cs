using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a task is assigned to a user and transitions to 'InProgress'.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="AssigneeId">The assigned user identifier.</param>
    /// <param name="PreviousStatus">The status of the task before assignment.</param>
    /// <param name="NewStatus">The new status after assignment (InProgress).</param>
    public sealed record TaskAssignedDomainEvent(
        Guid TaskId,
        Guid AssigneeId,
        TaskStatus PreviousStatus,
        TaskStatus NewStatus) : DomainEventBase(TaskId);
}
