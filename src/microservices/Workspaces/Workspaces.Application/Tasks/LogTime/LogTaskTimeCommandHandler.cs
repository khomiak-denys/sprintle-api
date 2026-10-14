using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.LogTime
{
    /// <summary>
    /// Handles <see cref="LogTaskTimeCommand"/> — appends a <see cref="Domain.Tasks.TimeLog"/> to the task and notifies the requester.
    /// </summary>
    internal sealed class LogTaskTimeCommandHandler : ICommandHandler<LogTaskTimeCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public LogTaskTimeCommandHandler(
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
        public async Task<Result> Handle(LogTaskTimeCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!task.AssigneeId.HasValue || task.AssigneeId.Value != command.UserId)
            {
                return Result.Failure(new ForbiddenError("Only the assigned user can log time on this task."));
            }

            var timeLog = task.LogTime(command.UserId, command.MinutesSpent, command.Description, command.LoggedDate);
            await _taskRepository.AddTimeLogAsync(timeLog, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            var workspace = await _workspaceRepository.GetByIdAsync(task.WorkspaceId, cancellationToken);
            var author = await _usersApiClient.GetByIdAsync(command.UserId, cancellationToken);
            var creator = await _usersApiClient.GetByIdAsync(task.CreatedById, cancellationToken);

            var workspaceName = workspace?.Name ?? "Workspace";
            var authorName = !string.IsNullOrWhiteSpace(author?.FullName) ? author.FullName : (!string.IsNullOrWhiteSpace(author?.UserName) ? author.UserName : "A team member");
            var creatorName = !string.IsNullOrWhiteSpace(creator?.FullName) ? creator.FullName : (!string.IsNullOrWhiteSpace(creator?.UserName) ? creator.UserName : (creator?.Email ?? "Requester"));
            var creatorEmail = creator?.Email ?? string.Empty;

            await _eventPublisher.PublishAsync(
                new TaskTimeLogged(
                    task.Id,
                    task.Title,
                    task.WorkspaceId,
                    workspaceName,
                    timeLog.Id,
                    command.MinutesSpent,
                    command.Description,
                    command.LoggedDate,
                    command.UserId,
                    authorName,
                    task.CreatedById,
                    creatorEmail,
                    creatorName),
                cancellationToken);

            return Result.Success();
        }
    }
}
