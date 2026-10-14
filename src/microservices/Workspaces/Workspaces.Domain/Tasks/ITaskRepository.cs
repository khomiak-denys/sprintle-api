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

        /// <summary>
        /// Loads a <see cref="TaskItem"/> aggregate root by rehydrating it from its stream of domain events.
        /// If no events are stored for this task, falls back to loading the state entity.
        /// </summary>
        /// <param name="id">Task identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The rehydrated task aggregate, or <c>null</c> if not found.</returns>
        Task<TaskItem?> GetByIdFromEventsAsync(Guid id, CancellationToken cancellationToken) => GetByIdAsync(id, cancellationToken);

        /// <summary>
        /// Saves uncommitted domain events for the aggregate into the event store,
        /// updates the Read Model projection, and clears uncommitted events.
        /// </summary>
        /// <param name="task">The task aggregate root.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveAggregateAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;

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

        /// <summary>Returns a comment by its id, or <c>null</c> if not found.</summary>
        /// <param name="commentId">Comment identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The comment, or <c>null</c> if not found.</returns>
        Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken) => Task.FromResult<TaskComment?>(null);

        /// <summary>Adds a new <see cref="TaskComment"/> to the persistence context asynchronously.</summary>
        /// <param name="comment">The comment to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Adds a new <see cref="TimeLog"/> to the persistence context asynchronously.</summary>
        /// <param name="timeLog">The time log to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddTimeLogAsync(TimeLog timeLog, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Removes a <see cref="TaskComment"/> from the persistence context asynchronously.</summary>
        /// <param name="comment">The comment to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveCommentAsync(TaskComment comment, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Returns a paginated list of comments for a given task, ordered by CreatedAt descending at the database level.</summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated result of task comments.</returns>
        Task<PaginationResult<TaskComment>> GetCommentsByTaskIdAsync(
            Guid taskId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(PaginationResult<TaskComment>.Create(Array.Empty<TaskComment>(), page, pageSize, 0));

        /// <summary>Returns comment counts for a batch of task IDs in a single query.</summary>
        /// <param name="taskIds">The task identifiers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary mapping task ID to its comment count.</returns>
        Task<IReadOnlyDictionary<Guid, int>> GetCommentCountsAsync(
            IReadOnlyCollection<Guid> taskIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
    }
}
