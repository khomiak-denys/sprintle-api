using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Workspaces.DTOs;

namespace Workspaces.Application.Workspaces.Members.GetMembers
{
    /// <summary>
    /// Query to retrieve paginated members of a workspace with optional search.
    /// Guarded so only admins or workspace members can view.
    /// </summary>
    /// <param name="WorkspaceId">The workspace identifier.</param>
    /// <param name="RequestingUserId">The current user identifier.</param>
    /// <param name="IsAdmin">Whether the current user has the Admin role.</param>
    /// <param name="Search">Optional search filter across username, full name, or email.</param>
    /// <param name="Page">Page number (1-based, defaults to 1).</param>
    /// <param name="PageSize">Number of items per page (defaults to 3).</param>
    public record GetWorkspaceMembersQuery(
        Guid WorkspaceId,
        Guid RequestingUserId,
        bool IsAdmin,
        string? Search = null,
        int Page = 1,
        int PageSize = 3) : IQuery<Result<PaginationResult<WorkspaceMemberResult>>>;
}
