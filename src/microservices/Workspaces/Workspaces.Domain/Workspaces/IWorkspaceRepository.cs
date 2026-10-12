using DomainFramework;

namespace Workspaces.Domain.Workspaces
{
    /// <summary>
    /// Persistence contract for the <see cref="Workspace"/> aggregate.
    /// Implemented in <c>Workspaces.Infrastructure</c>.
    /// </summary>
    public interface IWorkspaceRepository
    {
        /// <summary>Returns a <see cref="Workspace"/> by its id, including members.</summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The workspace, or <c>null</c> if not found.</returns>
        Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>Returns a paginated list of workspaces the specified user is a member of, optionally filtered by name.</summary>
        /// <param name="userId">The user whose workspaces to retrieve.</param>
        /// <param name="name">Optional name filter (case-insensitive contains).</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated result of workspaces.</returns>
        Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Returns a paginated list of all workspaces, optionally filtered by name.</summary>
        /// <param name="name">Optional name filter (case-insensitive contains).</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated result of workspaces.</returns>
        Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Adds a new <see cref="Workspace"/> to the persistence context.</summary>
        /// <param name="workspace">The workspace to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddAsync(Workspace workspace, CancellationToken cancellationToken);

        /// <summary>Removes a <see cref="Workspace"/> from the persistence context.</summary>
        /// <param name="workspace">The workspace to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveAsync(Workspace workspace, CancellationToken cancellationToken);

        /// <summary>Adds a new <see cref="WorkspaceMember"/> to the persistence context asynchronously.</summary>
        /// <param name="member">The workspace member to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddMemberAsync(WorkspaceMember member, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Removes a <see cref="WorkspaceMember"/> from the persistence context asynchronously.</summary>
        /// <param name="member">The workspace member to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveMemberAsync(WorkspaceMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
