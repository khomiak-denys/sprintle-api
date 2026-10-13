using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.Interfaces;

namespace Users.Application.Auth.RevokeRefreshToken
{
    /// <summary>
    /// Handles <see cref="RevokeRefreshTokenCommand"/> to revoke user refresh tokens.
    /// </summary>
    public class RevokeRefreshTokenCommandHandler : ICommandHandler<RevokeRefreshTokenCommand, Result>
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RevokeRefreshTokenCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public RevokeRefreshTokenCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        /// <inheritdoc />
        public Task<Result> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken)
        {
            return _authService.RevokeRefreshTokenAsync(command.UserId, cancellationToken);
        }
    }
}
