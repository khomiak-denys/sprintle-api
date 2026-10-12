using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.RemoveMember
{
    /// <summary>
    /// Handles <see cref="RemoveWorkspaceMemberCommand"/> — ensures authorization then removes the member.
    /// </summary>
    internal sealed class RemoveWorkspaceMemberCommandHandler : ICommandHandler<RemoveWorkspaceMemberCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly IWorkspaceRepository _workspaceRepository;

        public RemoveWorkspaceMemberCommandHandler(IUnitOfWork uow, IWorkspaceRepository workspaceRepository)
        {
            _uow = uow;
            _workspaceRepository = workspaceRepository;
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(RemoveWorkspaceMemberCommand command, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
            if (workspace is null)
            {
                return Result.Failure(new NotFoundError($"Workspace '{command.WorkspaceId}' was not found."));
            }

            var isOwner = workspace.OwnerId == command.RequestingUserId;
            var isSelfLeaving = command.MemberUserId == command.RequestingUserId;

            if (!command.IsAdmin && !isOwner && !isSelfLeaving)
            {
                return Result.Failure(new ForbiddenError("You are not allowed to remove members from this workspace."));
            }

            var removeResult = workspace.RemoveMember(command.MemberUserId);
            if (removeResult.IsFailure)
            {
                return Result.Failure(removeResult.Error);
            }

            await _workspaceRepository.RemoveMemberAsync(removeResult.Value, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
