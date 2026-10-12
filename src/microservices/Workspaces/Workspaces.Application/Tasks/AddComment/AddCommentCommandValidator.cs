using FluentValidation;

namespace Workspaces.Application.Tasks.AddComment
{
    /// <summary>
    /// Validates <see cref="AddCommentCommand"/> before execution.
    /// </summary>
    public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
    {
        /// <summary>Initializes a new instance of <see cref="AddCommentCommandValidator"/>.</summary>
        public AddCommentCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("TaskId must not be empty.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId must not be empty.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Comment content must not be empty.")
                .MaximumLength(2000)
                .WithMessage("Comment content must not exceed 2000 characters.");
        }
    }
}
