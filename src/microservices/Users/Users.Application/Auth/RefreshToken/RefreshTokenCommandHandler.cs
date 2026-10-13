using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Auth.RefreshToken
{
    /// <summary>
    /// Handles <see cref="RefreshTokenCommand"/> to refresh expired authentication tokens.
    /// </summary>
    public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthTokensResult>>
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public RefreshTokenCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        /// <inheritdoc />
        public Task<Result<AuthTokensResult>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            return _authService.RefreshTokenAsync(command.AccessToken, command.RefreshToken, cancellationToken);
        }
    }
}
