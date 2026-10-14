using System;
using System.Collections.Generic;
using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.GetStatusHistory
{
    /// <summary>
    /// Query to retrieve the event-sourced status history Read Model for a given task.
    /// </summary>
    /// <param name="TaskId">The unique identifier of the task.</param>
    public sealed record GetTaskStatusHistoryQuery(Guid TaskId) : IQuery<Result<IReadOnlyList<TaskStatusHistoryDto>>>;
}
