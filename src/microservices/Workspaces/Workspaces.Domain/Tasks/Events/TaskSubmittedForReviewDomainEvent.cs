using System;
using DomainFramework;

namespace Workspaces.Domain.Tasks.Events
{
    /// <summary>
    /// Domain event raised when a task is submitted for review and transitions from 'InProgress' to 'InReview'.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    /// <param name="PreviousStatus">The status before submission (InProgress).</param>
    /// <param name="NewStatus">The status after submission (InReview).</param>
    public sealed record TaskSubmittedForReviewDomainEvent(
        Guid TaskId,
        TaskStatus PreviousStatus,
        TaskStatus NewStatus) : DomainEventBase(TaskId);
}
