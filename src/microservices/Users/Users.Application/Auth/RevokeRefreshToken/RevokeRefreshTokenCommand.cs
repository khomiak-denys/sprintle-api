using DomainFramework.Results;
using Messaging.Abstractions;

namespace Users.Application.Auth.RevokeRefreshToken
{
    /// <summary>
    /// Command to revoke refresh tokens for a specified user.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user whose refresh token should be revoked.</param>
    public record RevokeRefreshTokenCommand(Guid UserId) : ICommand<Result>;
}
