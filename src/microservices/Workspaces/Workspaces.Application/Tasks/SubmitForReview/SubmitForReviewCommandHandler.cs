using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.SubmitForReview
{
    /// <summary>
    /// Handles <see cref="SubmitForReviewCommand"/> — validates that the caller is the assigned user and transitions the task to InReview.
    /// </summary>
    internal sealed class SubmitForReviewCommandHandler : ICommandHandler<SubmitForReviewCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;

        public SubmitForReviewCommandHandler(IUnitOfWork uow, ITaskRepository taskRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SubmitForReviewCommand command, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
            if (task is null)
            {
                return Result.Failure(new NotFoundError($"Task '{command.TaskId}' was not found."));
            }

            if (!task.AssigneeId.HasValue || task.AssigneeId.Value != command.RequestedById)
            {
                return Result.Failure(new ForbiddenError("Only the assigned user can submit the task for review."));
            }

            var result = task.SubmitForReview();
            if (result.IsFailure)
            {
                return result;
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
