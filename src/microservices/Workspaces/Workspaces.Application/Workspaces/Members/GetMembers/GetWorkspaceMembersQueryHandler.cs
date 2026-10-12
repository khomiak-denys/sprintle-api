using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.GetMembers
{
    /// <summary>
    /// Handles <see cref="GetWorkspaceMembersQuery"/> to retrieve paginated workspace members with details from Users API.
    /// Guarded so only workspace members or admins can view.
    /// </summary>
    internal sealed partial class GetWorkspaceMembersQueryHandler
        : IQueryHandler<GetWorkspaceMembersQuery, Result<PaginationResult<UserResult>>>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly ILogger<GetWorkspaceMembersQueryHandler> _logger;

        public GetWorkspaceMembersQueryHandler(
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient,
            ILogger<GetWorkspaceMembersQueryHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Result<PaginationResult<UserResult>>> Handle(
            GetWorkspaceMembersQuery query,
            CancellationToken cancellationToken)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(query.WorkspaceId, cancellationToken);
            if (workspace is null)
            {
                LogWorkspaceNotFound(query.WorkspaceId);
                return Result<PaginationResult<UserResult>>.Failure(
                    new NotFoundError($"Workspace '{query.WorkspaceId}' was not found."));
            }

            if (!query.IsAdmin)
            {
                var isMember = await _workspaceRepository.IsMemberAsync(query.WorkspaceId, query.RequestingUserId, cancellationToken);
                if (!isMember)
                {
                    LogUnauthorizedAccess(query.RequestingUserId, query.WorkspaceId);
                    return Result<PaginationResult<UserResult>>.Failure(
                        new ForbiddenError("Only workspace members or an admin can view workspace members."));
                }
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
                    isWorkspaceMember: true,
                    cancellationToken: cancellationToken);

                foreach (var (userId, userResult) in chunkMap)
                {
                    usersMap[userId] = userResult;
                }
            }

            var results = usersMap.Values.ToList();

            var totalCount = results.Count;
            var effectivePage = Math.Max(1, query.Page);
            var effectivePageSize = Math.Max(1, query.PageSize);

            var pagedItems = results
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var paginationResult = PaginationResult<UserResult>.Create(
                pagedItems,
                effectivePage,
                effectivePageSize,
                totalCount);

            return Result<PaginationResult<UserResult>>.Success(paginationResult);
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Workspace '{WorkspaceId}' was not found when retrieving members.")]
        private partial void LogWorkspaceNotFound(Guid workspaceId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "User '{UserId}' is not authorized to view members of workspace '{WorkspaceId}'.")]
        private partial void LogUnauthorizedAccess(Guid userId, Guid workspaceId);
    }
}
