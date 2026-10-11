using Workspaces.Application.Tasks.GetMy;

namespace Workspaces.API.Tasks.Requests
{
    /// <summary>
    /// Request model for querying tasks associated with the current user with optional filters and pagination.
    /// </summary>
    /// <param name="WorkspaceId">Optional workspace identifier filter.</param>
    /// <param name="Type">Optional filter type ('created', 'assigned', 'all', or null).</param>
    /// <param name="Search">Optional search term matching task title or description.</param>
    /// <param name="Status">Optional task status filter.</param>
    /// <param name="Priority">Optional task priority filter.</param>
    /// <param name="Tag">Optional tag name filter.</param>
    /// <param name="Page">The page number to retrieve (default is 1).</param>
    /// <param name="PageSize">The number of items per page (default is 10).</param>
    public record GetMyTasksRequest(
        Guid? WorkspaceId = null,
        string? Type = null,
        string? Search = null,
        string? Status = null,
        string? Priority = null,
        string? Tag = null,
        int Page = 1,
        int PageSize = 10)
    {
        /// <summary>
        /// Converts the request to a <see cref="GetMyTasksQuery"/>.
        /// </summary>
        /// <param name="userId">The current user identifier.</param>
        /// <returns>A new <see cref="GetMyTasksQuery"/> instance.</returns>
        public GetMyTasksQuery ToQuery(Guid userId)
        {
            return new GetMyTasksQuery(
                userId,
                WorkspaceId,
                Type,
                Search,
                Status,
                Priority,
                Tag,
                Page,
                PageSize);
        }
    }
}
