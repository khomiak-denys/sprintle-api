using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Tasks.DTOs;

namespace Workspaces.Application.Tasks.GetMy
{
    /// <summary>
    /// Query to retrieve tasks assigned to or created by the requesting user, with optional filters.
    /// </summary>
    /// <param name="UserId">The id of the user.</param>
    /// <param name="WorkspaceId">Optional workspace identifier filter.</param>
    /// <param name="Type">The filter type ('created', 'assigned', 'all', or null for all).</param>
    /// <param name="Search">Optional search term matching task title or description.</param>
    /// <param name="Status">Optional status filter (e.g. 'Todo', 'InProgress', 'InReview', 'Done', 'Cancelled', or 'all').</param>
    /// <param name="Priority">Optional priority filter (e.g. 'Low', 'Medium', 'High', 'Critical', or 'all').</param>
    /// <param name="Tag">Optional tag name filter.</param>
    /// <param name="Page">The page number.</param>
    /// <param name="PageSize">The page size.</param>
    public record GetMyTasksQuery(
        Guid UserId,
        Guid? WorkspaceId = null,
        string? Type = null,
        string? Search = null,
        string? Status = null,
        string? Priority = null,
        string? Tag = null,
        int Page = 1,
        int PageSize = 10) : IQuery<Result<PaginationResult<TaskResult>>>;
}
