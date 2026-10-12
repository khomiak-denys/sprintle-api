using FluentValidation;

namespace Workspaces.Application.Tasks.Close
{
    /// <summary>
    /// Validates <see cref="CloseTaskCommand"/> before execution.
    /// </summary>
    public class CloseTaskCommandValidator : AbstractValidator<CloseTaskCommand>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CloseTaskCommandValidator"/>.
        /// </summary>
        public CloseTaskCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.RequestedById).NotEmpty();
        }
    }
}
