using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.GetById
{
    /// <summary>
    /// Handles <see cref="GetTaskByIdQuery"/> — retrieves task details including enriched user information.
    /// </summary>
    internal sealed class GetTaskByIdQueryHandler : IQueryHandler<GetTaskByIdQuery, Result<TaskDetailsResult>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetTaskByIdQueryHandler(
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient)
        {
            _taskRepository = taskRepository;
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
        }

        public async Task<Result<TaskDetailsResult>> Handle(GetTaskByIdQuery query, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(query.TaskId, cancellationToken);
            if (task is null)
            {
                return Result<TaskDetailsResult>.Failure(new NotFoundError($"Task '{query.TaskId}' was not found."));
            }

            if (!query.IsAdmin)
            {
                var isMember = await _workspaceRepository.IsMemberAsync(task.WorkspaceId, query.RequestingUserId, cancellationToken);
                if (!isMember)
                {
                    return Result<TaskDetailsResult>.Failure(new ForbiddenError("You are not allowed to view this task."));
                }
            }

            var tags = task.TaskTags
                .Where(tt => tt.Tag is not null)
                .Select(tt => tt.Tag.Name)
                .ToList();

            var timeLogs = task.TimeLogs
                .Select(l => new TimeLogResult(
                    l.Id,
                    l.UserId,
                    l.MinutesSpent,
                    l.Description,
                    l.LoggedDate,
                    l.CreatedAt))
                .ToList();

            var createdUserTask = _usersApiClient.GetByIdAsync(task.CreatedById, cancellationToken);
            var assignedUserTask = task.AssigneeId.HasValue
                ? _usersApiClient.GetByIdAsync(task.AssigneeId.Value, cancellationToken)
                : Task.FromResult<UserResult?>(null);

            await Task.WhenAll(createdUserTask, assignedUserTask);

            var createdUser = await createdUserTask
                ?? new UserResult(task.CreatedById, string.Empty, "Unknown", null);
            var assignedUser = await assignedUserTask;

            var availableTransitions = ComputeTransitions(task, query.RequestingUserId, query.IsAdmin);

            var result = new TaskDetailsResult(
                task.Id,
                task.WorkspaceId,
                task.Number,
                task.Title,
                task.Description,
                task.Type,
                task.Status,
                task.Priority,
                task.Deadline,
                assignedUser,
                createdUser,
                task.CreatedAt,
                task.UpdatedAt,
                tags,
                timeLogs,
                availableTransitions);

            return Result<TaskDetailsResult>.Success(result);
        }

        private static List<TaskTransitionResult> ComputeTransitions(
            TaskItem task,
            Guid requestingUserId,
            bool isAdmin)
        {
            var transitions = new List<TaskTransitionResult>();
            var isRequesterOrAdmin = isAdmin || task.CreatedById == requestingUserId;

            foreach (var action in task.GetAvailableTransitions())
            {
                switch (action)
                {
                    case TaskAction.Assign:
                        transitions.Add(new TaskTransitionResult(TaskAction.Assign, Domain.Tasks.TaskStatus.InProgress, true));
                        break;

                    case TaskAction.SubmitForReview:
                        var isAssignee = task.AssigneeId.HasValue && task.AssigneeId.Value == requestingUserId;
                        transitions.Add(new TaskTransitionResult(TaskAction.SubmitForReview, Domain.Tasks.TaskStatus.InReview, isAssignee));
                        break;

                    case TaskAction.Approve:
                        transitions.Add(new TaskTransitionResult(TaskAction.Approve, Domain.Tasks.TaskStatus.Done, isRequesterOrAdmin));
                        break;

                    case TaskAction.Close:
                        transitions.Add(new TaskTransitionResult(TaskAction.Close, Domain.Tasks.TaskStatus.Closed, isRequesterOrAdmin));
                        break;

                    case TaskAction.Cancel:
                        transitions.Add(new TaskTransitionResult(TaskAction.Cancel, Domain.Tasks.TaskStatus.Cancelled, isRequesterOrAdmin));
                        break;
                }
            }

            return transitions;
        }
    }
}
