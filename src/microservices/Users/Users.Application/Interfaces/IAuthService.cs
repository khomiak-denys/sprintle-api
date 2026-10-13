using DomainFramework.Results;
using Users.Application.DTOs;

namespace Users.Application.Interfaces
{
    /// <summary>
    /// Contract for authentication and authorization operations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user with the specified credentials.
        /// </summary>
        /// <param name="email">The user email.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The user password.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result containing authentication tokens or an error.</returns>
        Task<Result<AuthTokensResult>> RegisterAsync(string email, string userName, string password, CancellationToken ct);

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        /// <param name="email">The user email.</param>
        /// <param name="password">The user password.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result containing authentication tokens or an error.</returns>
        Task<Result<AuthTokensResult>> LoginAsync(string email, string password, CancellationToken ct);

        /// <summary>
        /// Refreshes access and refresh tokens.
        /// </summary>
        /// <param name="accessToken">The expired access token.</param>
        /// <param name="refreshToken">The valid refresh token.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result containing new authentication tokens or an error.</returns>
        Task<Result<AuthTokensResult>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct);

        /// <summary>
        /// Revokes the refresh token for the specified user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct);

        /// <summary>
        /// Generates an email confirmation token for the user with the specified email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result containing user ID, email, and confirmation token, or an error.</returns>
        Task<Result<EmailConfirmationTokenResult>> GenerateEmailConfirmationTokenAsync(string email, CancellationToken ct);

        /// <summary>
        /// Confirms a user's email address using a confirmation token.
        /// </summary>
        /// <param name="token">The email confirmation token.</param>
        /// <param name="userId">The optional user ID.</param>
        /// <param name="email">The optional user email.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A result containing confirmed user info or an error.</returns>
        Task<Result<ConfirmedUserResult>> FinishEmailConfirmationAsync(string token, Guid? userId, string? email, CancellationToken ct);
    }
}
