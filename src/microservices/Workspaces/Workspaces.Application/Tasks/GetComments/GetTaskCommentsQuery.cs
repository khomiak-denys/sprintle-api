using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Tasks.DTOs;

namespace Workspaces.Application.Tasks.GetComments
{
    /// <summary>
    /// Query to retrieve paginated comments for a specific task.
    /// </summary>
    /// <param name="TaskId">The task identifier.</param>
    /// <param name="RequestingUserId">The user making the request.</param>
    /// <param name="IsAdmin">Whether the requesting user is an admin.</param>
    /// <param name="Page">Page number (1-based).</param>
    /// <param name="PageSize">Number of items per page.</param>
    public record GetTaskCommentsQuery(
        Guid TaskId,
        Guid RequestingUserId,
        bool IsAdmin,
        int Page,
        int PageSize) : IQuery<Result<PaginationResult<TaskCommentResult>>>;
}
