using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.SubmitForReview
{
    /// <summary>
    /// Handles <see cref="SubmitForReviewCommand"/> — validates that the caller is the assigned user and transitions the task to InReview.
    /// </summary>
    internal sealed class SubmitForReviewCommandHandler : ICommandHandler<SubmitForReviewCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public SubmitForReviewCommandHandler(
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
        public async Task<Result> Handle(SubmitForReviewCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdFromEventsAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!task.AssigneeId.HasValue || task.AssigneeId.Value != command.RequestedById)
            {
                return Result.Failure(new ForbiddenError("Only the assigned user can submit the task for review."));
            }

            var oldStatus = task.Status.ToString();
            var result = task.SubmitForReview();
            if (result.IsFailure)
            {
                return result;
            }

            await _taskRepository.SaveAggregateAsync(task, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var workspace = await _workspaceRepository.GetByIdAsync(task.WorkspaceId, cancellationToken);
            var creator = await _usersApiClient.GetByIdAsync(task.CreatedById, cancellationToken);
            var changer = await _usersApiClient.GetByIdAsync(command.RequestedById, cancellationToken);

            var workspaceName = workspace?.Name ?? "Workspace";
            var changerName = !string.IsNullOrWhiteSpace(changer?.FullName) ? changer.FullName : (!string.IsNullOrWhiteSpace(changer?.UserName) ? changer.UserName : "A team member");
            var creatorName = !string.IsNullOrWhiteSpace(creator?.FullName) ? creator.FullName : (!string.IsNullOrWhiteSpace(creator?.UserName) ? creator.UserName : (creator?.Email ?? "Requester"));
            var creatorEmail = creator?.Email ?? string.Empty;

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
                    changer?.Email,
                    changerName,
                    TaskNumber: task.Number),
                cancellationToken);

            return Result.Success();
        }
    }
}
