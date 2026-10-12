using FluentValidation;

namespace Workspaces.Application.Tasks.DeleteComment
{
    /// <summary>
    /// Validates <see cref="DeleteCommentCommand"/> before execution.
    /// </summary>
    public class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
    {
        /// <summary>Initializes a new instance of <see cref="DeleteCommentCommandValidator"/>.</summary>
        public DeleteCommentCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("TaskId must not be empty.");

            RuleFor(x => x.CommentId)
                .NotEmpty()
                .WithMessage("CommentId must not be empty.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId must not be empty.");
        }
    }
}
