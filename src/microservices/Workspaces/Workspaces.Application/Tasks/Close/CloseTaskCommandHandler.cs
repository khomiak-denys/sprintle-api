using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.Close
{
    /// <summary>
    /// Handles <see cref="CloseTaskCommand"/> — validates permissions and transitions task to Closed.
    /// </summary>
    internal sealed class CloseTaskCommandHandler : ICommandHandler<CloseTaskCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public CloseTaskCommandHandler(
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

        /// <inheritdoc/>
        public async Task<Result> Handle(CloseTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!command.IsAdmin && task.CreatedById != command.RequestedById)
            {
                return Result.Failure(new ForbiddenError("Only an administrator or the task creator can close this task."));
            }

            var oldStatus = task.Status.ToString();
            var result = task.Close();
            if (result.IsFailure)
            {
                return result;
            }

            await _uow.SaveChangesAsync(cancellationToken);

            var workspace = await _workspaceRepository.GetByIdAsync(task.WorkspaceId, cancellationToken);
            var creator = await _usersApiClient.GetByIdAsync(task.CreatedById, cancellationToken);
            var assignee = task.AssigneeId.HasValue
                ? await _usersApiClient.GetByIdAsync(task.AssigneeId.Value, cancellationToken)
                : null;
            var changer = await _usersApiClient.GetByIdAsync(command.RequestedById, cancellationToken);

            var workspaceName = workspace?.Name ?? "Workspace";
            var changerName = !string.IsNullOrWhiteSpace(changer?.FullName) ? changer.FullName : (!string.IsNullOrWhiteSpace(changer?.UserName) ? changer.UserName : "A team member");
            var creatorName = !string.IsNullOrWhiteSpace(creator?.FullName) ? creator.FullName : (!string.IsNullOrWhiteSpace(creator?.UserName) ? creator.UserName : (creator?.Email ?? "Requester"));
            var creatorEmail = creator?.Email ?? string.Empty;
            var assigneeName = !string.IsNullOrWhiteSpace(assignee?.FullName) ? assignee.FullName : (!string.IsNullOrWhiteSpace(assignee?.UserName) ? assignee.UserName : assignee?.Email);
            var assigneeEmail = assignee?.Email;

            await _eventPublisher.PublishAsync(
                new TaskStatusChanged(
                    task.Id,
                    task.Title,
                    task.WorkspaceId,
                    workspaceName,
                    oldStatus,
                    task.Status.ToString(),
                    command.RequestedById,
                    changerName,
                    task.CreatedById,
                    creatorEmail,
                    creatorName,
                    task.AssigneeId,
                    assigneeEmail,
                    assigneeName),
                cancellationToken);

            return Result.Success();
        }
    }
}
