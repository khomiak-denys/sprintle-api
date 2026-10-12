namespace Workspaces.Application.Workspaces.DTOs
{
    /// <summary>
    /// Represents a potential member who can be added to a workspace with user details from the Users service.
    /// </summary>
    /// <param name="UserId">The potential member's unique user identifier.</param>
    /// <param name="Email">The potential member's email address.</param>
    /// <param name="UserName">The potential member's username.</param>
    /// <param name="FullName">The potential member's full name, if available.</param>
    public record PotentialWorkspaceMemberResult(
        Guid UserId,
        string Email,
        string UserName,
        string? FullName)
    {
        /// <summary>
        /// Gets the unique user identifier (alias for <see cref="UserId"/>).
        /// </summary>
        public Guid Id => UserId;
    }
}
