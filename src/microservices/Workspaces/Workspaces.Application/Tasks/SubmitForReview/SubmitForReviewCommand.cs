using DomainFramework.Results;
using Messaging.Abstractions;

namespace Workspaces.Application.Tasks.SubmitForReview
{
    /// <summary>
    /// Command to submit a task for review (InProgress → InReview).
    /// Allowed only for the assigned user.
    /// </summary>
    /// <param name="TaskId">The task to submit for review.</param>
    /// <param name="RequestedById">Id of the authenticated user performing the submission.</param>
    public record SubmitForReviewCommand(
        Guid TaskId,
        Guid RequestedById) : ICommand<Result>;
}
