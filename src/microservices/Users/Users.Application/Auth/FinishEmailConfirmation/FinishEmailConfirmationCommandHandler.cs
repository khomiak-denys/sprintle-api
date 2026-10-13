using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.Interfaces;

namespace Users.Application.Auth.FinishEmailConfirmation
{
    /// <summary>
    /// Handles <see cref="FinishEmailConfirmationCommand"/> to verify email confirmation token and publish <see cref="UserEmailConfirmed"/>.
    /// </summary>
    public class FinishEmailConfirmationCommandHandler : ICommandHandler<FinishEmailConfirmationCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="FinishEmailConfirmationCommandHandler"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="eventPublisher">The message bus event publisher.</param>
        public FinishEmailConfirmationCommandHandler(IAuthService authService, IEventPublisher eventPublisher)
        {
            _authService = authService;
            _eventPublisher = eventPublisher;
        }

        /// <inheritdoc />
        public async Task<Result> Handle(FinishEmailConfirmationCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.FinishEmailConfirmationAsync(
                command.Token,
                command.UserId,
                command.Email,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error);
            }

            await _eventPublisher.PublishAsync(
                new UserEmailConfirmed(result.Value.UserId, result.Value.Email),
                cancellationToken);

            return Result.Success();
        }
    }
}
