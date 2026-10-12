using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.LogTime
{
    /// <summary>
    /// Handles <see cref="LogTaskTimeCommand"/> — appends a <see cref="Domain.Tasks.TimeLog"/> to the task.
    /// </summary>
    internal sealed class LogTaskTimeCommandHandler : ICommandHandler<LogTaskTimeCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;

        public LogTaskTimeCommandHandler(IUnitOfWork uow, ITaskRepository taskRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(LogTaskTimeCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            var timeLog = task.LogTime(command.UserId, command.MinutesSpent, command.Description, command.LoggedDate);
            await _taskRepository.AddTimeLogAsync(timeLog, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
