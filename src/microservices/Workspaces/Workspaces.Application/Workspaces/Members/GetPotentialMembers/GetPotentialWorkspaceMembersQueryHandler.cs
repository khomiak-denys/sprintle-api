using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Workspaces.DTOs;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.GetPotentialMembers
{
    /// <summary>
    /// Handles <see cref="GetPotentialWorkspaceMembersQuery"/> to retrieve paginated potential workspace members from Users API.
    /// Guarded so only the workspace owner or an admin can view.
    /// </summary>
    internal sealed class GetPotentialWorkspaceMembersQueryHandler
        : IQueryHandler<GetPotentialWorkspaceMembersQuery, Result<PaginationResult<WorkspaceMemberResult>>>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetPotentialWorkspaceMembersQueryHandler(
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient)
        {
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
        }

        /// <inheritdoc/>
        public async Task<Result<PaginationResult<WorkspaceMemberResult>>> Handle(
            GetPotentialWorkspaceMembersQuery query,
            CancellationToken cancellationToken)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(query.WorkspaceId, cancellationToken);
            if (workspace is null)
            {
                return Result<PaginationResult<WorkspaceMemberResult>>.Failure(
                    new NotFoundError($"Workspace '{query.WorkspaceId}' was not found."));
            }

            var isOwner = workspace.OwnerId == query.RequestingUserId;
            if (!query.IsAdmin && !isOwner)
            {
                return Result<PaginationResult<WorkspaceMemberResult>>.Failure(
                    new ForbiddenError("Only the workspace owner or an admin can view potential members."));
            }

            var currentMemberUserIds = workspace.Members
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            var usersMap = await _usersApiClient.GetBatchAsync(
                currentMemberUserIds,
                page: query.Page,
                pageSize: query.PageSize,
                search: query.Search,
                isWorkspaceMember: false,
                cancellationToken: cancellationToken);

            var items = usersMap.Values
                .Select(u => new WorkspaceMemberResult(u.Id, u.Email, u.UserName, u.FullName, IsOwner: false))
                .ToList();

            var paginationResult = PaginationResult<WorkspaceMemberResult>.Create(
                items,
                query.Page,
                query.PageSize,
                items.Count);

            return Result<PaginationResult<WorkspaceMemberResult>>.Success(paginationResult);
        }
    }
}
