using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;

namespace Workspaces.Domain.Workspaces
{
    /// <summary>
    /// Represents a workspace that groups tasks and members together.
    /// Inherits auditing fields (Id, CreatedAt, UpdatedAt) from <see cref="EntityBase"/>.
    /// </summary>
    public class Workspace : EntityBase
    {
        /// <summary>Gets the workspace name.</summary>
        public string Name { get; private set; } = null!;

        /// <summary>Gets the unique identifier of the workspace owner.</summary>
        public Guid OwnerId { get; private set; }

        /// <summary>Gets the optional workspace description.</summary>
        public string? Description { get; private set; }

        /// <summary>Gets the workspace members (join entities).</summary>
        public ICollection<WorkspaceMember> Members { get; private set; } = new List<WorkspaceMember>();

        /// <summary>EF Core parameterless constructor.</summary>
        private Workspace() { }

        private Workspace(string name, Guid ownerId, string? description)
        {
            Name = name;
            OwnerId = ownerId;
            Description = description;
        }

        /// <summary>
        /// Factory method that creates a new <see cref="Workspace"/> and adds the owner as a member.
        /// </summary>
        /// <param name="name">Non-empty workspace name (max 200 chars).</param>
        /// <param name="ownerId">The user id of the workspace owner.</param>
        /// <param name="description">Optional description (max 2000 chars).</param>
        /// <returns>A fully initialised workspace with the owner registered as a member.</returns>
        public static Workspace Create(string name, Guid ownerId, string? description)
        {
            var workspace = new Workspace(name, ownerId, description);
            workspace.OnCreate();

            // Owner is automatically a member.
            workspace.Members.Add(WorkspaceMember.Create(workspace.Id, ownerId));

            return workspace;
        }

        /// <summary>
        /// Updates mutable workspace properties.
        /// </summary>
        /// <param name="name">New name.</param>
        /// <param name="description">New description.</param>
        public void Update(string name, string? description)
        {
            Name = name;
            Description = description;
            OnModify();
        }

        /// <summary>
        /// Adds a user to the workspace membership list.
        /// </summary>
        /// <param name="userId">The user id to add.</param>
        /// <returns>A success result with the created member, or failure if the user is already a member.</returns>
        public Result<WorkspaceMember> AddMember(Guid userId)
        {
            if (Members.Any(m => m.UserId == userId))
            {
                return Result<WorkspaceMember>.Failure(new AlreadyExistsError($"User '{userId}' is already a member of this workspace."));
            }

            var member = WorkspaceMember.Create(Id, userId);
            Members.Add(member);
            OnModify();
            return Result<WorkspaceMember>.Success(member);
        }

        /// <summary>
        /// Removes a user from the workspace membership list.
        /// </summary>
        /// <param name="userId">The user id to remove.</param>
        /// <returns>A success result with the removed member, or failure if the user is not a member or is the owner.</returns>
        public Result<WorkspaceMember> RemoveMember(Guid userId)
        {
            if (userId == OwnerId)
            {
                return Result<WorkspaceMember>.Failure(new InvalidArgumentError("Cannot remove the workspace owner from membership."));
            }

            var member = Members.FirstOrDefault(m => m.UserId == userId);
            if (member is null)
            {
                return Result<WorkspaceMember>.Failure(new NotFoundError($"User '{userId}' is not a member of this workspace."));
            }

            Members.Remove(member);
            OnModify();
            return Result<WorkspaceMember>.Success(member);
        }
    }
}
