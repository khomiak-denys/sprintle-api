using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.Interfaces;

namespace Users.Application.Auth.ConfirmEmail
{
    /// <summary>
    /// Handles <see cref="ConfirmEmailCommand"/> to generate an email confirmation token and publish <see cref="UserEmailConfirmationRequested"/>.
    /// </summary>
    public class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmEmailCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="eventPublisher">The message bus event publisher.</param>
        public ConfirmEmailCommandHandler(IAuthService authService, IEventPublisher eventPublisher)
        {
            _authService = authService;
            _eventPublisher = eventPublisher;
        }

        /// <inheritdoc />
        public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.GenerateEmailConfirmationTokenAsync(command.Email, cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error);
            }

            await _eventPublisher.PublishAsync(
                new UserEmailConfirmationRequested(result.Value.UserId, result.Value.Email, result.Value.Token),
                cancellationToken);

            return Result.Success();
        }
    }
}
