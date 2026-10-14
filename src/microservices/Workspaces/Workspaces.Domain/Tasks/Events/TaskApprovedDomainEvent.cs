using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a task is approved and transitions from 'InReview' to 'Done'.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="PreviousStatus">The status before approval (InReview).</param>
    /// <param name="NewStatus">The status after approval (Done).</param>
    public sealed record TaskApprovedDomainEvent(
        Guid TaskId,
        TaskStatus PreviousStatus,
        TaskStatus NewStatus) : DomainEventBase(TaskId);
}
