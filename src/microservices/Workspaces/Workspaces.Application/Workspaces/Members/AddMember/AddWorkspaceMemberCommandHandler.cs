using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.AddMember
{
    /// <summary>
    /// Handles <see cref="AddWorkspaceMemberCommand"/> — ensures authorization then adds the member.
    /// </summary>
    internal sealed class AddWorkspaceMemberCommandHandler : ICommandHandler<AddWorkspaceMemberCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly IWorkspaceRepository _workspaceRepository;

        public AddWorkspaceMemberCommandHandler(IUnitOfWork uow, IWorkspaceRepository workspaceRepository)
        {
            _uow = uow;
            _workspaceRepository = workspaceRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(AddWorkspaceMemberCommand command, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
            if (workspace is null)
            {
                return Result.Failure(new NotFoundError($"Workspace '{command.WorkspaceId}' was not found."));
            }

            if (!command.IsAdmin && workspace.OwnerId != command.RequestingUserId)
            {
                return Result.Failure(new ForbiddenError("Only the workspace owner or an admin can add members."));
            }

            var isAlreadyMember = await _workspaceRepository.IsMemberAsync(command.WorkspaceId, command.MemberUserId, cancellationToken);
            if (isAlreadyMember)
            {
                return Result.Failure(new AlreadyExistsError($"User '{command.MemberUserId}' is already a member of this workspace."));
            }

            var addResult = workspace.AddMember(command.MemberUserId);
            if (addResult.IsFailure)
            {
                return Result.Failure(addResult.Error);
            }

            await _workspaceRepository.AddMemberAsync(addResult.Value, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
