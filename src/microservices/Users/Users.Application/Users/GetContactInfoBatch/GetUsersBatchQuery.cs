using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;

namespace Users.Application.Users.GetContactInfoBatch
{
    /// <summary>
    /// Query to retrieve paginated contact information for a batch of user identifiers with optional search and workspace membership filter.
    /// </summary>
    /// <param name="UserIds">The collection of user unique identifiers to look up or exclude.</param>
    /// <param name="Search">Optional search filter across username, full name, or email.</param>
    /// <param name="IsWorkspaceMember">
    /// Optional workspace membership flag. When <c>false</c>, returns non-admin users excluding <paramref name="UserIds"/>;
    /// when <c>true</c> or <c>null</c>, returns users matching <paramref name="UserIds"/>.
    /// </param>
    /// <param name="Page">The page number (1-based).</param>
    /// <param name="PageSize">The page size.</param>
    public record GetUsersBatchQuery(
        IReadOnlyList<Guid> UserIds,
        string? Search = null,
        bool? IsWorkspaceMember = null,
        int Page = 1,
        int PageSize = 10) : IQuery<Result<PaginationResult<UserContactInfoResult>>>;
}
