using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Tasks.Events;

namespace Workspaces.Application.Tasks.GetStatusHistory
{
    /// <summary>
    /// Handles <see cref="GetTaskStatusHistoryQuery"/> to project the task's event stream into a status history Read Model.
    /// </summary>
    internal sealed class GetTaskStatusHistoryQueryHandler : IQueryHandler<GetTaskStatusHistoryQuery, Result<IReadOnlyList<TaskStatusHistoryDto>>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskEventStore _eventStore;

        public GetTaskStatusHistoryQueryHandler(ITaskRepository taskRepository, ITaskEventStore eventStore)
        {
            _taskRepository = taskRepository;
            _eventStore = eventStore;
        }

        public async Task<Result<IReadOnlyList<TaskStatusHistoryDto>>> Handle(
            GetTaskStatusHistoryQuery query,
            CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(query.TaskId, cancellationToken);
            if (task is null)
            {
                return Result<IReadOnlyList<TaskStatusHistoryDto>>.Failure(
                    new NotFoundError($"Task '{query.TaskId}' was not found."));
            }

            var events = await _eventStore.GetEventsAsync(query.TaskId, cancellationToken);
            var history = new List<TaskStatusHistoryDto>();
            var version = 1;

            foreach (var @event in events)
            {
                switch (@event)
                {
                    case TaskCreatedDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskCreatedDomainEvent),
                            null,
                            e.InitialStatus.ToString(),
                            e.OccurredAt));
                        break;

                    case TaskAssignedDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskAssignedDomainEvent),
                            e.PreviousStatus.ToString(),
                            e.NewStatus.ToString(),
                            e.OccurredAt));
                        break;

                    case TaskSubmittedForReviewDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskSubmittedForReviewDomainEvent),
                            e.PreviousStatus.ToString(),
                            e.NewStatus.ToString(),
                            e.OccurredAt));
                        break;

                    case TaskApprovedDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskApprovedDomainEvent),
                            e.PreviousStatus.ToString(),
                            e.NewStatus.ToString(),
                            e.OccurredAt));
                        break;

                    case TaskClosedDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskClosedDomainEvent),
                            e.PreviousStatus.ToString(),
                            e.NewStatus.ToString(),
                            e.OccurredAt));
                        break;

                    case TaskCancelledDomainEvent e:
                        history.Add(new TaskStatusHistoryDto(
                            e.EventId,
                            e.TaskId,
                            version++,
                            nameof(TaskCancelledDomainEvent),
                            e.PreviousStatus.ToString(),
                            e.NewStatus.ToString(),
                            e.OccurredAt));
                        break;
                }
            }

            return Result<IReadOnlyList<TaskStatusHistoryDto>>.Success(history);
        }
    }
}
