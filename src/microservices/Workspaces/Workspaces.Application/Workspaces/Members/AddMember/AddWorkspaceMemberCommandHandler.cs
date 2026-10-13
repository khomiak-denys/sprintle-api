using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Workspaces.Members.AddMember
{
    /// <summary>
    /// Handles <see cref="AddWorkspaceMemberCommand"/> — ensures authorization, adds the member, and publishes <see cref="WorkspaceMemberAdded"/>.
    /// </summary>
    internal sealed class AddWorkspaceMemberCommandHandler : ICommandHandler<AddWorkspaceMemberCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddWorkspaceMemberCommandHandler"/> class.
        /// </summary>
        /// <param name="uow">The unit of work for database persistence.</param>
        /// <param name="workspaceRepository">The workspace repository.</param>
        /// <param name="usersApiClient">The users API client for retrieving member and manager contact info.</param>
        /// <param name="eventPublisher">The message bus event publisher.</param>
        public AddWorkspaceMemberCommandHandler(
            IUnitOfWork uow,
            IWorkspaceRepository workspaceRepository,
            IUsersApiClient usersApiClient,
            IEventPublisher eventPublisher)
        {
            _uow = uow;
            _workspaceRepository = workspaceRepository;
            _usersApiClient = usersApiClient;
            _eventPublisher = eventPublisher;
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

            var memberUser = await _usersApiClient.GetByIdAsync(command.MemberUserId, cancellationToken);
            if (memberUser is not null && !string.IsNullOrWhiteSpace(memberUser.Email))
            {
                var managerUser = await _usersApiClient.GetByIdAsync(command.RequestingUserId, cancellationToken);
                var managerName = !string.IsNullOrWhiteSpace(managerUser?.FullName)
                    ? managerUser.FullName
                    : (!string.IsNullOrWhiteSpace(managerUser?.UserName) ? managerUser.UserName : "Workspace Manager");

                var memberName = !string.IsNullOrWhiteSpace(memberUser.FullName)
                    ? memberUser.FullName
                    : memberUser.UserName;

                await _eventPublisher.PublishAsync(
                    new WorkspaceMemberAdded(
                        workspace.Id,
                        workspace.Name,
                        command.MemberUserId,
                        memberUser.Email,
                        memberName,
                        command.RequestingUserId,
                        managerName),
                    cancellationToken);
            }

            return Result.Success();
        }
    }
}
