using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.Assign
{
    /// <summary>
    /// Handles <see cref="AssignTaskCommand"/> — assigns a task to an assignee.
    /// </summary>
    internal sealed class AssignTaskCommandHandler : ICommandHandler<AssignTaskCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public AssignTaskCommandHandler(
            IUnitOfWork uow,
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient,
            IEventPublisher eventPublisher)
        {
            _uow = uow;
            _taskRepository = taskRepository;
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
            _eventPublisher = eventPublisher;
        }

        public async Task<Result> Handle(AssignTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            var user = await _usersApiClient.GetByIdAsync(command.AssigneeId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(new NotFoundError($"User '{command.AssigneeId}' was not found."));
            }

            var isMember = await _workspaceRepository.IsMemberAsync(task.WorkspaceId, command.AssigneeId, cancellationToken);
            if (!isMember)
            {
                return Result.Failure(new InvalidArgumentError("The assigned user must be a member of the workspace."));
            }

            var assignResult = task.Assign(command.AssigneeId);
            if (assignResult.IsFailure)
            {
                return assignResult;
            }

            await _uow.SaveChangesAsync(cancellationToken);

            var workspace = await _workspaceRepository.GetByIdAsync(task.WorkspaceId, cancellationToken);
            var assigner = await _usersApiClient.GetByIdAsync(command.RequestedById, cancellationToken);

            var workspaceName = workspace?.Name ?? "Workspace";
            var assigneeName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (!string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : user.Email);
            var assignerName = !string.IsNullOrWhiteSpace(assigner?.FullName) ? assigner.FullName : (!string.IsNullOrWhiteSpace(assigner?.UserName) ? assigner.UserName : "A team member");

            await _eventPublisher.PublishAsync(
                new TaskAssigned(
                    task.Id,
                    task.Title,
                    task.Description,
                    task.WorkspaceId,
                    workspaceName,
                    user.Id,
                    user.Email,
                    assigneeName,
                    command.RequestedById,
                    assignerName,
                    task.Priority.ToString(),
                    task.Deadline),
                cancellationToken);

            return Result.Success();
        }
    }
}
