using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.GetAll
{
    /// <summary>
    /// Handles <see cref="GetAllTasksQuery"/> — retrieves paged tasks matching filters.
    /// </summary>
    internal sealed class GetAllTasksQueryHandler : IQueryHandler<GetAllTasksQuery, Result<PaginationResult<TaskResult>>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetAllTasksQueryHandler(ITaskRepository taskRepository, IUsersApiClient usersApiClient)
        {
            _taskRepository = taskRepository;
            _usersApiClient = usersApiClient;
        }

        public async Task<Result<PaginationResult<TaskResult>>> Handle(GetAllTasksQuery query, CancellationToken cancellationToken)
        {
            TaskStatus? status = !string.IsNullOrWhiteSpace(query.Status) && !query.Status.Equals("all", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<TaskStatus>(query.Status, true, out var parsedStatus)
                ? parsedStatus
                : null;

            Priority? priority = !string.IsNullOrWhiteSpace(query.Priority) && !query.Priority.Equals("all", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<Priority>(query.Priority, true, out var parsedPriority)
                ? parsedPriority
                : null;

            Guid? effectiveAssigneeId = query.AssigneeId;
            Guid? effectiveCreatedById = query.CreatedById;

            if (query.RequestingUserId.HasValue && query.RequestingUserId.Value != Guid.Empty && !string.IsNullOrWhiteSpace(query.Type))
            {
                if (query.Type.Equals("assigned", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveAssigneeId = query.RequestingUserId.Value;
                }
                else if (query.Type.Equals("created", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveCreatedById = query.RequestingUserId.Value;
                }
            }

            var pagedTasks = await _taskRepository.GetAllAsync(
                query.WorkspaceId,
                query.Search,
                status,
                priority,
                effectiveAssigneeId,
                effectiveCreatedById,
                query.Tag,
                query.Page,
                query.PageSize,
                cancellationToken);

            IReadOnlyDictionary<Guid, UserResult> assigneeMap = new Dictionary<Guid, UserResult>();
            var distinctAssigneeIds = pagedTasks.Items
                .Where(t => t.AssigneeId.HasValue)
                .Select(t => t.AssigneeId!.Value)
                .Distinct()
                .ToList();

            if (distinctAssigneeIds.Count > 0)
            {
                assigneeMap = await _usersApiClient.GetBatchAsync(
                    distinctAssigneeIds,
                    1,
                    Math.Clamp(distinctAssigneeIds.Count, 1, 100),
                    cancellationToken);
            }

            var items = pagedTasks.Items
                .Select(task => new TaskResult(
                    task.Id,
                    task.WorkspaceId,
                    task.Title,
                    task.Description,
                    task.Status,
                    task.Priority,
                    task.Deadline,
                    task.TaskTags
                        .Where(tt => tt.Tag is not null)
                        .Select(tt => tt.Tag.Name)
                        .ToList(),
                    task.AssigneeId.HasValue && assigneeMap.TryGetValue(task.AssigneeId.Value, out var assignee) ? assignee : null))
                .ToList();

            var result = PaginationResult<TaskResult>.Create(
                items,
                pagedTasks.Page,
                pagedTasks.PageSize,
                pagedTasks.TotalCount);

            return Result<PaginationResult<TaskResult>>.Success(result);
        }
    }
}
