using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.AddComment
{
    /// <summary>
    /// Handles <see cref="AddCommentCommand"/> — adds a comment to a task.
    /// </summary>
    internal sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;

        public AddCommentCommandHandler(IUnitOfWork uow, ITaskRepository taskRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc/>
        public async Task<Result<Guid>> Handle(AddCommentCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result<Guid>.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!command.IsAdmin && task.CreatedById != command.UserId && task.AssigneeId != command.UserId)
            {
                return Result<Guid>.Failure(new ForbiddenError("You are not allowed to comment on this task."));
            }

            var comment = task.AddComment(command.UserId, command.Content);
            await _taskRepository.AddCommentAsync(comment, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(comment.Id);
        }
    }
}
