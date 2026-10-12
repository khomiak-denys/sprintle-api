using FluentValidation;

namespace Workspaces.Application.Tasks.SubmitForReview
{
    /// <summary>
    /// Validates <see cref="SubmitForReviewCommand"/> before execution.
    /// </summary>
    public class SubmitForReviewCommandValidator : AbstractValidator<SubmitForReviewCommand>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SubmitForReviewCommandValidator"/>.
        /// </summary>
        public SubmitForReviewCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.RequestedById).NotEmpty();
        }
    }
}
