using DomainFramework;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Persistence contract for the <see cref="TaskItem"/> aggregate.
    /// Implemented in <c>Workspaces.Infrastructure</c>.
    /// </summary>
    public interface ITaskRepository
    {
        /// <summary>Returns a <see cref="TaskItem"/> by its id, including tags and time logs.</summary>
        /// <param name="id">Task identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The task, or <c>null</c> if not found.</returns>
        Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>Returns a paginated list of all tasks matching optional filters.</summary>
        /// <param name="workspaceId">Optional workspace identifier filter.</param>
        /// <param name="search">Optional search term matching task title or description.</param>
        /// <param name="status">Optional task status.</param>
        /// <param name="priority">Optional task priority.</param>
        /// <param name="assigneeId">Optional assignee identifier filter.</param>
        /// <param name="createdById">Optional creator identifier filter.</param>
        /// <param name="tag">Optional tag name filter.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated result of tasks.</returns>
        Task<PaginationResult<TaskItem>> GetAllAsync(
            Guid? workspaceId,
            string? search,
            TaskStatus? status,
            Priority? priority,
            Guid? assigneeId,
            Guid? createdById,
            string? tag,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        /// <summary>Returns a paginated list of tasks filtered by creator, assignee, or both, with optional additional filters.</summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="workspaceId">Optional workspace identifier filter.</param>
        /// <param name="type">Filter type: "created", "assigned", "all", or null/empty for both.</param>
        /// <param name="search">Optional search term matching task title or description.</param>
        /// <param name="status">Optional task status.</param>
        /// <param name="priority">Optional task priority.</param>
        /// <param name="tag">Optional tag name filter.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated result of tasks.</returns>
        Task<PaginationResult<TaskItem>> GetMyAsync(
            Guid userId,
            Guid? workspaceId,
            string? type,
            string? search,
            TaskStatus? status,
            Priority? priority,
            string? tag,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        /// <summary>Adds a new <see cref="TaskItem"/> to the persistence context asynchronously.</summary>
        /// <param name="task">The task to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddAsync(TaskItem task, CancellationToken cancellationToken);

        /// <summary>Removes a <see cref="TaskItem"/> from the persistence context asynchronously.</summary>
        /// <param name="task">The task to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveAsync(TaskItem task, CancellationToken cancellationToken);

        /// <summary>Returns the total count of tasks belonging to a workspace.</summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Count of matching tasks.</returns>
        Task<int> GetCountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken) => Task.FromResult(0);
    }
}
