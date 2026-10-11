namespace Workspaces.Application.Workspaces.DTOs
{
    /// <summary>
    /// Represents a member of a workspace with user details from the Users service.
    /// </summary>
    /// <param name="UserId">The member's unique user identifier.</param>
    /// <param name="Email">The member's email address.</param>
    /// <param name="UserName">The member's username.</param>
    /// <param name="FullName">The member's full name, if available.</param>
    /// <param name="IsOwner">Whether the member is the owner of the workspace.</param>
    public record WorkspaceMemberResult(
        Guid UserId,
        string Email,
        string UserName,
        string? FullName,
        bool IsOwner)
    {
        /// <summary>
        /// Gets the unique user identifier (alias for <see cref="UserId"/>).
        /// </summary>
        public Guid Id => UserId;
    }
}
