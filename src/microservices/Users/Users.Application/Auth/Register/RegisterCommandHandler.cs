using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Auth.Register
{
    /// <summary>
    /// Handles <see cref="RegisterCommand"/> to register a new user.
    /// </summary>
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthTokensResult>>
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public RegisterCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        /// <inheritdoc />
        public async Task<Result<AuthTokensResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
        {
            return await _authService.RegisterAsync(command.Email, command.UserName, command.Password, cancellationToken);
        }
    }
}
