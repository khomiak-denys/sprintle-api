using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a user is added to a workspace by an owner or manager.
    /// </summary>
    /// <param name="WorkspaceId">The unique identifier of the workspace.</param>
    /// <param name="WorkspaceName">The display name of the workspace.</param>
    /// <param name="MemberUserId">The unique identifier of the added member.</param>
    /// <param name="MemberEmail">The email address of the added member.</param>
    /// <param name="MemberName">The display name or username of the added member.</param>
    /// <param name="AddedByUserId">The unique identifier of the manager or owner who added the member.</param>
    /// <param name="AddedByName">The display name or username of the manager or owner who added the member.</param>
    public sealed record WorkspaceMemberAdded(
        Guid WorkspaceId,
        string WorkspaceName,
        Guid MemberUserId,
        string MemberEmail,
        string MemberName,
        Guid AddedByUserId,
        string AddedByName);
}
