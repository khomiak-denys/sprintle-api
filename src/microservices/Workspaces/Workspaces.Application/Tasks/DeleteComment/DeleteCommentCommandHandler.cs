using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.DeleteComment
{
    /// <summary>
    /// Handles <see cref="DeleteCommentCommand"/> — removes a comment from a task.
    /// </summary>
    internal sealed class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;

        public DeleteCommentCommandHandler(IUnitOfWork uow, ITaskRepository taskRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(DeleteCommentCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            var comment = await _taskRepository.GetCommentByIdAsync(command.CommentId, cancellationToken);
            if (comment is null || comment.TaskId != command.TaskId)
            {
                return Result.Failure(new NotFoundError($"Comment '{command.CommentId}' was not found."));
            }

            if (comment.AuthorId != command.UserId && !command.IsAdmin)
            {
                return Result.Failure(new ForbiddenError("Only the comment author or an admin can delete a comment."));
            }

            await _taskRepository.RemoveCommentAsync(comment, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
