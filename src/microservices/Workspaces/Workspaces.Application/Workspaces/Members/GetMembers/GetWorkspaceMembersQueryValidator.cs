using FluentValidation;

namespace Workspaces.Application.Workspaces.Members.GetMembers
{
    /// <summary>
    /// Validates <see cref="GetWorkspaceMembersQuery"/> before execution.
    /// </summary>
    public class GetWorkspaceMembersQueryValidator : AbstractValidator<GetWorkspaceMembersQuery>
    {
        /// <summary>Initializes a new instance of <see cref="GetWorkspaceMembersQueryValidator"/>.</summary>
        public GetWorkspaceMembersQueryValidator()
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
