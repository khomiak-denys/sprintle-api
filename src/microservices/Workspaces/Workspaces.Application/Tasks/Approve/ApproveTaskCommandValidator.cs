using FluentValidation;

namespace Workspaces.Application.Tasks.Approve
{
    /// <summary>
    /// Validates <see cref="ApproveTaskCommand"/> before execution.
    /// </summary>
    public class ApproveTaskCommandValidator : AbstractValidator<ApproveTaskCommand>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApproveTaskCommandValidator"/>.
        /// </summary>
        public ApproveTaskCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.RequestedById).NotEmpty();
        }
    }
}
