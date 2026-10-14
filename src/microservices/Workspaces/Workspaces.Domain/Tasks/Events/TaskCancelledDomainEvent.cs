using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a task is cancelled and transitions to 'Cancelled'.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="PreviousStatus">The status of the task before cancellation.</param>
    /// <param name="NewStatus">The status after cancellation (Cancelled).</param>
    public sealed record TaskCancelledDomainEvent(
        Guid TaskId,
        TaskStatus PreviousStatus,
        TaskStatus NewStatus) : DomainEventBase(TaskId);
}
