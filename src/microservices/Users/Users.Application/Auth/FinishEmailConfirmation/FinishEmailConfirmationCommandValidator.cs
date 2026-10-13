using FluentValidation;

namespace Users.Application.Auth.FinishEmailConfirmation
{
    /// <summary>
    /// Validator for <see cref="FinishEmailConfirmationCommand"/>.
    /// </summary>
    public class FinishEmailConfirmationCommandValidator : AbstractValidator<FinishEmailConfirmationCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FinishEmailConfirmationCommandValidator"/> class.
        /// </summary>
        public FinishEmailConfirmationCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty();

            RuleFor(x => x)
                .Must(x => (x.UserId.HasValue && x.UserId.Value != Guid.Empty) || !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Either UserId or Email must be provided.");

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email)
                    .EmailAddress();
            });
        }
    }
}
