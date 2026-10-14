using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a completed task is closed and transitions from 'Done' to 'Closed'.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="PreviousStatus">The status before closing (Done).</param>
    /// <param name="NewStatus">The status after closing (Closed).</param>
    public sealed record TaskClosedDomainEvent(
        Guid TaskId,
        TaskStatus PreviousStatus,
        TaskStatus NewStatus) : DomainEventBase(TaskId);
}
