using DomainFramework;
using DomainFramework.Results;
using Users.Application.DTOs;

namespace Users.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserProfileResult>> GetByIdAsync(Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves contact information for a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The user's contact information if found; otherwise, a failure result.</returns>
        Task<Result<UserContactInfoResult>> GetContactInfoByIdAsync(Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves paginated contact information for a collection of user identifiers with optional search and workspace membership filter.
        /// </summary>
        /// <param name="userIds">The collection of user identifiers.</param>
        /// <param name="search">Optional search term matching username, full name, or email.</param>
        /// <param name="isWorkspaceMember">
        /// Optional workspace membership flag. When <c>false</c>, returns non-admin users excluding <paramref name="userIds"/>;
        /// when <c>true</c> or <c>null</c>, returns users matching <paramref name="userIds"/>.
        /// </param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A paginated list of user contact information.</returns>
        Task<Result<PaginationResult<UserContactInfoResult>>> GetContactInfoBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            string? search,
            bool? isWorkspaceMember,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<Result<PaginationResult<UserResult>>> GetAllAsync(int page, int pageSize, CancellationToken ct);
        Task<Result> UpdateProfileAsync(Guid userId, string? fullName, string? userName, CancellationToken ct);
        Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);
        Task<Result> DeleteAsync(Guid userId, CancellationToken ct);
    }
}
