using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.GetMy
{
    /// <summary>
    /// Handles <see cref="GetMyTasksQuery"/> — retrieves paged tasks for the user.
    /// </summary>
    internal sealed class GetMyTasksQueryHandler : IQueryHandler<GetMyTasksQuery, Result<PaginationResult<TaskResult>>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetMyTasksQueryHandler(ITaskRepository taskRepository, IUsersApiClient usersApiClient)
        {
            _taskRepository = taskRepository;
            _usersApiClient = usersApiClient;
        }

        public async Task<Result<PaginationResult<TaskResult>>> Handle(GetMyTasksQuery query, CancellationToken cancellationToken)
        {
            TaskStatus? status = !string.IsNullOrWhiteSpace(query.Status) && !query.Status.Equals("all", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<TaskStatus>(query.Status, true, out var parsedStatus)
                ? parsedStatus
                : null;

            Priority? priority = !string.IsNullOrWhiteSpace(query.Priority) && !query.Priority.Equals("all", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<Priority>(query.Priority, true, out var parsedPriority)
                ? parsedPriority
                : null;

            var pagedTasks = await _taskRepository.GetMyAsync(
                query.UserId,
                query.WorkspaceId,
                query.Type,
                query.Search,
                status,
                priority,
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
                    cancellationToken: cancellationToken);
            }

            var taskIds = pagedTasks.Items.Select(t => t.Id).ToList();
            var commentCounts = await _taskRepository.GetCommentCountsAsync(taskIds, cancellationToken);

            var items = pagedTasks.Items
                .Select(task => new TaskResult(
                    task.Id,
                    task.WorkspaceId,
                    task.Number,
                    task.Title,
                    task.Description,
                    task.Type,
                    task.Status,
                    task.Priority,
                    task.Deadline,
                    task.TaskTags
                        .Where(tt => tt.Tag is not null)
                        .Select(tt => tt.Tag.Name)
                        .ToList(),
                    task.AssigneeId.HasValue && assigneeMap.TryGetValue(task.AssigneeId.Value, out var assignee) ? assignee : null,
                    commentCounts.TryGetValue(task.Id, out var count) ? count : 0))
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
