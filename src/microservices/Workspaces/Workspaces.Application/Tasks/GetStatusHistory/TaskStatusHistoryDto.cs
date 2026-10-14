using System;

namespace Workspaces.Application.Tasks.GetStatusHistory
{
    /// <summary>
    /// Represents a single status transition record within the task's event stream.
    /// </summary>
    /// <param name="EventId">Unique event identifier.</param>
    /// <param name="TaskId">The task aggregate identifier.</param>
    /// <param name="Version">The sequence version number in the event stream.</param>
    /// <param name="EventType">The domain event type name.</param>
    /// <param name="PreviousStatus">The status before the transition, if applicable.</param>
    /// <param name="NewStatus">The resulting status after the transition.</param>
    /// <param name="OccurredAt">The UTC timestamp when the transition occurred.</param>
    public sealed record TaskStatusHistoryDto(
        Guid EventId,
        Guid TaskId,
        int Version,
        string EventType,
        string? PreviousStatus,
        string NewStatus,
        DateTime OccurredAt);
}
