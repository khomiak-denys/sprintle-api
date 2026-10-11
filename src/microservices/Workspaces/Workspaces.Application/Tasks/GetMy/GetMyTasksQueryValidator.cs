using FluentValidation;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Application.Tasks.GetMy
{
    /// <summary>
    /// Validates <see cref="GetMyTasksQuery"/> before execution.
    /// </summary>
    public class GetMyTasksQueryValidator : AbstractValidator<GetMyTasksQuery>
    {
        /// <summary>Initializes a new instance of <see cref="GetMyTasksQueryValidator"/>.</summary>
        public GetMyTasksQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId must not be empty.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.Type)
                .Must(t => string.IsNullOrEmpty(t) ||
                           t.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                           t.Equals("created", StringComparison.OrdinalIgnoreCase) ||
                           t.Equals("assigned", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Type must be either 'all', 'created', 'assigned', or omitted.");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) ||
                           s.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                           Enum.TryParse<TaskStatus>(s, true, out _))
                .WithMessage("Status must be a valid task status or 'all'.");

            RuleFor(x => x.Priority)
                .Must(p => string.IsNullOrEmpty(p) ||
                           p.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                           Enum.TryParse<Priority>(p, true, out _))
                .WithMessage("Priority must be a valid priority or 'all'.");

            RuleFor(x => x.WorkspaceId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("WorkspaceId must not be empty when provided.");
        }
    }
}
