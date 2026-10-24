using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.AddComment
{
    /// <summary>
    /// Handles <see cref="AddCommentCommand"/> — adds a comment to a task and notifies interested parties.
    /// </summary>
    internal sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public AddCommentCommandHandler(
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

            var workspace = await _workspaceRepository.GetByIdAsync(task.WorkspaceId, cancellationToken);
            var author = await _usersApiClient.GetByIdAsync(command.UserId, cancellationToken);
            var creator = await _usersApiClient.GetByIdAsync(task.CreatedById, cancellationToken);
            var assignee = task.AssigneeId.HasValue
                ? await _usersApiClient.GetByIdAsync(task.AssigneeId.Value, cancellationToken)
                : null;

            var workspaceName = workspace?.Name ?? "Workspace";
            var authorName = !string.IsNullOrWhiteSpace(author?.FullName) ? author.FullName : (!string.IsNullOrWhiteSpace(author?.UserName) ? author.UserName : "A team member");
            var creatorName = !string.IsNullOrWhiteSpace(creator?.FullName) ? creator.FullName : (!string.IsNullOrWhiteSpace(creator?.UserName) ? creator.UserName : (creator?.Email ?? "Requester"));
            var creatorEmail = creator?.Email ?? string.Empty;
            var assigneeName = !string.IsNullOrWhiteSpace(assignee?.FullName) ? assignee.FullName : (!string.IsNullOrWhiteSpace(assignee?.UserName) ? assignee.UserName : assignee?.Email);
            var assigneeEmail = assignee?.Email;

            await _eventPublisher.PublishAsync(
                new TaskCommentAdded(
                    task.Id,
                    task.Title,
                    task.WorkspaceId,
                    workspaceName,
                    comment.Id,
                    comment.Content,
                    command.UserId,
                    authorName,
                    task.CreatedById,
                    creatorEmail,
                    creatorName,
                    task.AssigneeId,
                    assigneeEmail,
                    assigneeName,
                    TaskNumber: task.Number),
                cancellationToken);

            return Result<Guid>.Success(comment.Id);
        }
    }
}
