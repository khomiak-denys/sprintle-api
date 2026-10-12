using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.Assign
{
    /// <summary>
    /// Handles <see cref="AssignTaskCommand"/> — assigns a task to an assignee.
    /// </summary>
    internal sealed class AssignTaskCommandHandler : ICommandHandler<AssignTaskCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IUsersApiClient _usersApiClient;

        public AssignTaskCommandHandler(
            IUnitOfWork uow,
            ITaskRepository taskRepository,
            IUsersApiClient usersApiClient)
        {
            _uow = uow;
            _taskRepository = taskRepository;
            _usersApiClient = usersApiClient;
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

            var assignResult = task.Assign(command.AssigneeId);
            if (assignResult.IsFailure)
            {
                return assignResult;
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
