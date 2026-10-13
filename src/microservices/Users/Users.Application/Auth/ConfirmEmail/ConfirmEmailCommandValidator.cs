using FluentValidation;

namespace Users.Application.Auth.ConfirmEmail
{
    /// <summary>
    /// Validator for <see cref="ConfirmEmailCommand"/>.
    /// </summary>
    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmEmailCommandValidator"/> class.
        /// </summary>
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
