using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.AddComment
{
    /// <summary>
    /// Handles <see cref="AddCommentCommand"/> — adds a comment to a task.
    /// </summary>
    internal sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;

        public AddCommentCommandHandler(
            IUnitOfWork uow,
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
            _workspaceRepository = workspaceRepository;
        }

        /// <inheritdoc/>
        public async Task<Result<Guid>> Handle(AddCommentCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result<Guid>.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!command.IsAdmin)
            {
                var isMember = await _workspaceRepository.IsMemberAsync(task.WorkspaceId, command.UserId, cancellationToken);
                if (!isMember)
                {
                    return Result<Guid>.Failure(new ForbiddenError("You are not allowed to comment on this task."));
                }
            }

            var comment = task.AddComment(command.UserId, command.Content);
            await _taskRepository.AddCommentAsync(comment, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(comment.Id);
        }
    }
}
