namespace Users.Application.Auth
{
    /// <summary>
    /// Persistence contract for user authentication tokens.
    /// Implemented in <c>Users.Infrastructure</c>.
    /// </summary>
    public interface IUserTokenRepository
    {
        /// <summary>
        /// Returns the stored refresh token value and its expiry for the given user,
        /// or <c>null</c> if no token exists.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A tuple of (value, expiresAt), or <c>null</c> if not found.</returns>
        Task<(string Value, DateTime ExpiresAt)?> GetRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates or replaces the refresh token for the given user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="value">The raw refresh token value.</param>
        /// <param name="createdAt">UTC timestamp of issuance.</param>
        /// <param name="expiresAt">UTC timestamp of expiry.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpsertRefreshTokenAsync(Guid userId, string value, DateTime createdAt, DateTime expiresAt, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the refresh token for the given user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);
    }
}
