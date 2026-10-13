using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Auth.Register
{
    /// <summary>
    /// Handles <see cref="RegisterCommand"/> to register a new user and fires <see cref="UserRegistered"/>.
    /// </summary>
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthTokensResult>>
    {
        private readonly IAuthService _authService;
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="eventPublisher">The message bus event publisher.</param>
        public RegisterCommandHandler(IAuthService authService, IEventPublisher eventPublisher)
        {
            _authService = authService;
            _eventPublisher = eventPublisher;
        }

        /// <inheritdoc />
        public async Task<Result<AuthTokensResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(command.Email, command.UserName, command.Password, cancellationToken);

            if (result.IsSuccess)
            {
                await _eventPublisher.PublishAsync(
                    new UserRegistered(result.Value.UserId, command.UserName, command.Email),
                    cancellationToken);
            }

            return result;
        }
    }
}
