using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Workspaces.DTOs;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.GetMembers
{
    /// <summary>
    /// Handles <see cref="GetWorkspaceMembersQuery"/> to retrieve paginated workspace members with details from Users API.
    /// Guarded so only workspace members or admins can view.
    /// </summary>
    internal sealed class GetWorkspaceMembersQueryHandler
        : IQueryHandler<GetWorkspaceMembersQuery, Result<PaginationResult<WorkspaceMemberResult>>>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetWorkspaceMembersQueryHandler(
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient)
        {
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
        }

        /// <inheritdoc/>
        public async Task<Result<PaginationResult<WorkspaceMemberResult>>> Handle(
            GetWorkspaceMembersQuery query,
            CancellationToken cancellationToken)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(query.WorkspaceId, cancellationToken);
            if (workspace is null)
            {
                return Result<PaginationResult<WorkspaceMemberResult>>.Failure(
                    new NotFoundError($"Workspace '{query.WorkspaceId}' was not found."));
            }

            var isMemberOrOwner = workspace.OwnerId == query.RequestingUserId ||
                                 workspace.Members.Any(m => m.UserId == query.RequestingUserId);

            if (!query.IsAdmin && !isMemberOrOwner)
            {
                return Result<PaginationResult<WorkspaceMemberResult>>.Failure(
                    new ForbiddenError("Only workspace members or an admin can view workspace members."));
            }

            var memberUserIds = workspace.Members
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            var usersMap = new Dictionary<Guid, UserResult>();
            foreach (var chunk in memberUserIds.Chunk(100))
            {
                var chunkMap = await _usersApiClient.GetBatchAsync(
                    chunk,
                    page: 1,
                    pageSize: chunk.Length,
                    search: query.Search,
                    cancellationToken: cancellationToken);

                foreach (var (userId, userResult) in chunkMap)
                {
                    usersMap[userId] = userResult;
                }
            }

            var members = workspace.Members.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                members = members.Where(m => usersMap.ContainsKey(m.UserId));
            }

            var results = members
                .Select(m =>
                {
                    var hasUser = usersMap.TryGetValue(m.UserId, out var user);
                    return new WorkspaceMemberResult(
                        m.UserId,
                        hasUser ? user!.Email : string.Empty,
                        hasUser ? user!.UserName : "Unknown",
                        hasUser ? user!.FullName : null,
                        m.UserId == workspace.OwnerId);
                })
                .ToList();

            var totalCount = results.Count;
            var effectivePage = Math.Max(1, query.Page);
            var effectivePageSize = Math.Max(1, query.PageSize);

            var pagedItems = results
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var paginationResult = PaginationResult<WorkspaceMemberResult>.Create(
                pagedItems,
                effectivePage,
                effectivePageSize,
                totalCount);

            return Result<PaginationResult<WorkspaceMemberResult>>.Success(paginationResult);
        }
    }
}
