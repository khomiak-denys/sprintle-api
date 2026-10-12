using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.ChangePriority
{
    /// <summary>
    /// Handles <see cref="ChangeTaskPriorityCommand"/> — changes a task's priority level.
    /// </summary>
    internal sealed class ChangeTaskPriorityCommandHandler : ICommandHandler<ChangeTaskPriorityCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;

        public ChangeTaskPriorityCommandHandler(IUnitOfWork uow, ITaskRepository taskRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(ChangeTaskPriorityCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!command.IsAdmin && task.CreatedById != command.RequestedById)
            {
                return Result.Failure(new ForbiddenError("Only an administrator or the task requester can change the priority."));
            }

            task.ChangePriority(command.Priority);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
