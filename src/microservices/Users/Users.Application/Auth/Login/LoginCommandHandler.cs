using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Auth.Login
{
    /// <summary>
    /// Handles <see cref="LoginCommand"/> to authenticate a user.
    /// </summary>
    public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthTokensResult>>
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        /// <inheritdoc />
        public Task<Result<AuthTokensResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            return _authService.LoginAsync(command.Email, command.Password, cancellationToken);
        }
    }
}
