using FluentValidation;

namespace Workspaces.Application.Workspaces.Members.GetPotentialMembers
{
    /// <summary>
    /// Validates <see cref="GetPotentialWorkspaceMembersQuery"/> before execution.
    /// </summary>
    public class GetPotentialWorkspaceMembersQueryValidator : AbstractValidator<GetPotentialWorkspaceMembersQuery>
    {
        /// <summary>Initializes a new instance of <see cref="GetPotentialWorkspaceMembersQueryValidator"/>.</summary>
        public GetPotentialWorkspaceMembersQueryValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty()
                .WithMessage("WorkspaceId is required.");

            RuleFor(x => x.RequestingUserId)
                .NotEmpty()
                .WithMessage("RequestingUserId is required.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}
