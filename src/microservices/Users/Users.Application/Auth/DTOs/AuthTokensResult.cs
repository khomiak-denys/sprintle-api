namespace Users.Application.DTOs
{
    /// <summary>
    /// Represents authentication tokens and associated user identity information.
    /// </summary>
    /// <param name="AccessToken">The JWT access token.</param>
    /// <param name="RefreshToken">The refresh token.</param>
    /// <param name="ExpiresAt">The expiration timestamp of the access token.</param>
    /// <param name="UserId">The unique identifier of the authenticated user.</param>
    public record AuthTokensResult(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid UserId = default);
}
