using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;

namespace Users.Application.Auth.RefreshToken
{
    /// <summary>
    /// Command to refresh authentication tokens using an expired access token and refresh token.
    /// </summary>
    /// <param name="AccessToken">The expired JWT access token.</param>
    /// <param name="RefreshToken">The refresh token.</param>
    public record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommand<Result<AuthTokensResult>>;
}
