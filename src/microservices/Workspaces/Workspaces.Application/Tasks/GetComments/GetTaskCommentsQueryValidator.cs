using FluentValidation;

namespace Workspaces.Application.Tasks.GetComments
{
    /// <summary>
    /// Validates <see cref="GetTaskCommentsQuery"/> before execution.
    /// </summary>
    public class GetTaskCommentsQueryValidator : AbstractValidator<GetTaskCommentsQuery>
    {
        /// <summary>Initializes a new instance of <see cref="GetTaskCommentsQueryValidator"/>.</summary>
        public GetTaskCommentsQueryValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("TaskId must not be empty.");

            RuleFor(x => x.RequestingUserId)
                .NotEmpty()
                .WithMessage("RequestingUserId must not be empty.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}
