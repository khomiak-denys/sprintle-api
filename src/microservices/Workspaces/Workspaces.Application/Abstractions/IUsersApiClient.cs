using Workspaces.Application.Tasks.DTOs;

namespace Workspaces.Application.Abstractions
{
    /// <summary>
    /// HTTP client abstraction for communicating with the Users microservice.
    /// </summary>
    public interface IUsersApiClient
    {
        /// <summary>
        /// Retrieves user details by identifier from the Users API.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user details if found; otherwise, <c>null</c>.</returns>
        Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves user details for a batch of user identifiers from the Users API with pagination.
        /// </summary>
        /// <param name="userIds">The collection of unique user identifiers.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page (1 to 100).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A dictionary mapping user IDs to their user details.</returns>
        Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);
    }
}
