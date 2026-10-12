using FluentValidation;

namespace Workspaces.Application.Tasks.ChangePriority
{
    /// <summary>
    /// Validates <see cref="ChangeTaskPriorityCommand"/> before the handler executes.
    /// </summary>
    public class ChangeTaskPriorityCommandValidator : AbstractValidator<ChangeTaskPriorityCommand>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ChangeTaskPriorityCommandValidator"/>.
        /// </summary>
        public ChangeTaskPriorityCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty().WithMessage("TaskId is required.");

            RuleFor(x => x.RequestedById)
                .NotEmpty().WithMessage("RequestedById is required.");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Priority must be a valid priority level.");
        }
    }
}
