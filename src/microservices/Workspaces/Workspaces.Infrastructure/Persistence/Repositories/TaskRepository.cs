using DomainFramework;
using Microsoft.EntityFrameworkCore;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;

namespace Workspaces.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="ITaskRepository"/>.
    /// </summary>
    internal sealed class TaskRepository : ITaskRepository
    {
        private readonly WorkspacesDbContext _context;
        private readonly ITaskEventStore _eventStore;

        public TaskRepository(WorkspacesDbContext context, ITaskEventStore eventStore)
        {
            _context = context;
            _eventStore = eventStore;
        }

        /// <inheritdoc/>
        public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Tasks
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.TimeLogs)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TaskItem?> GetByIdFromEventsAsync(Guid id, CancellationToken cancellationToken)
        {
            var events = await _eventStore.GetEventsAsync(id, cancellationToken);
            if (events.Count == 0)
            {
                return await GetByIdAsync(id, cancellationToken);
            }

            var task = TaskItem.Rehydrate(id, events);

            var stateTask = await _context.Tasks
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.TimeLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (stateTask is not null)
            {
                foreach (var tag in stateTask.TaskTags)
                {
                    task.TaskTags.Add(tag);
                }

                foreach (var log in stateTask.TimeLogs)
                {
                    task.TimeLogs.Add(log);
                }
            }

            return task;
        }

        /// <inheritdoc/>
        public async Task<PaginationResult<TaskItem>> GetAllAsync(
            Guid? workspaceId,
            string? search,
            TaskStatus? status,
            Priority? priority,
            Guid? assigneeId,
            Guid? createdById,
            string? tag,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _context.Tasks
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .AsNoTracking();

            if (workspaceId.HasValue && workspaceId.Value != Guid.Empty)
            {
                query = query.Where(t => t.WorkspaceId == workspaceId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = ApplySearch(query, search);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == assigneeId.Value);
            }

            if (createdById.HasValue)
            {
                query = query.Where(t => t.CreatedById == createdById.Value);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var trimmedTag = tag.Trim();
                query = query.Where(t => t.TaskTags.Any(tt => tt.Tag != null && EF.Functions.ILike(tt.Tag.Name, $"%{trimmedTag}%")));
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginationResult<TaskItem>.Create(items, page, pageSize, totalCount);
        }

        /// <inheritdoc/>
        public async Task<PaginationResult<TaskItem>> GetMyAsync(
            Guid userId,
            Guid? workspaceId,
            string? type,
            string? search,
            TaskStatus? status,
            Priority? priority,
            string? tag,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _context.Tasks
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .AsNoTracking();

            if (workspaceId.HasValue && workspaceId.Value != Guid.Empty)
            {
                query = query.Where(t => t.WorkspaceId == workspaceId.Value);
            }

            if (string.Equals(type, "created", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.CreatedById == userId);
            }
            else if (string.Equals(type, "assigned", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.AssigneeId == userId);
            }
            else
            {
                // 'all', null, empty, or default: user is either creator or assignee
                query = query.Where(t => t.CreatedById == userId || t.AssigneeId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = ApplySearch(query, search);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var trimmedTag = tag.Trim();
                query = query.Where(t => t.TaskTags.Any(tt => tt.Tag != null && EF.Functions.ILike(tt.Tag.Name, $"%{trimmedTag}%")));
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginationResult<TaskItem>.Create(items, page, pageSize, totalCount);
        }

        /// <inheritdoc/>
        public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            if (task.UncommittedEvents.Count > 0)
            {
                var expectedVersion = task.Version - task.UncommittedEvents.Count;
                await _eventStore.AppendEventsAsync(task.Id, expectedVersion, task.UncommittedEvents, cancellationToken);
                task.ClearUncommittedEvents();
            }

            await _context.Tasks.AddAsync(task, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveAggregateAsync(TaskItem task, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(task);

            if (task.UncommittedEvents.Count > 0)
            {
                var expectedVersion = task.Version - task.UncommittedEvents.Count;
                await _eventStore.AppendEventsAsync(task.Id, expectedVersion, task.UncommittedEvents, cancellationToken);
                task.ClearUncommittedEvents();
            }

            var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, cancellationToken);
            if (existingTask is null)
            {
                await _context.Tasks.AddAsync(task, cancellationToken);
            }
            else if (!ReferenceEquals(existingTask, task))
            {
                _context.Entry(existingTask).CurrentValues.SetValues(task);
            }
        }

        /// <inheritdoc/>
        public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken)
        {
            _context.Tasks.Remove(task);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
        {
            return await _context.Tasks
                .AsNoTracking()
                .CountAsync(t => t.WorkspaceId == workspaceId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken)
        {
            return await _context.TaskComments
                .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken)
        {
            await _context.TaskComments.AddAsync(comment, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddTimeLogAsync(TimeLog timeLog, CancellationToken cancellationToken)
        {
            await _context.TimeLogs.AddAsync(timeLog, cancellationToken);
        }

        /// <inheritdoc/>
        public Task RemoveCommentAsync(TaskComment comment, CancellationToken cancellationToken)
        {
            _context.TaskComments.Remove(comment);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<PaginationResult<TaskComment>> GetCommentsByTaskIdAsync(
            Guid taskId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _context.TaskComments
                .AsNoTracking()
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginationResult<TaskComment>.Create(items, page, pageSize, totalCount);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<Guid, int>> GetCommentCountsAsync(
            IReadOnlyCollection<Guid> taskIds,
            CancellationToken cancellationToken)
        {
            if (taskIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            var counts = await _context.TaskComments
                .AsNoTracking()
                .Where(c => taskIds.Contains(c.TaskId))
                .GroupBy(c => c.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count, cancellationToken);

            return counts;
        }

        /// <summary>
        /// Applies the free-text search filter: matches title, description, or task number (e.g. <c>DMS-730</c>, <c>730</c>, or <c>#DMS-730</c>).
        /// </summary>
        private static IQueryable<TaskItem> ApplySearch(IQueryable<TaskItem> query, string search)
        {
            var trimmed = search.Trim();
            var cleanTerm = trimmed.TrimStart('#');

            return query.Where(t =>
                EF.Functions.ILike(t.Number, $"%{cleanTerm}%") ||
                EF.Functions.ILike(t.Title, $"%{trimmed}%") ||
                (t.Description != null && EF.Functions.ILike(t.Description, $"%{trimmed}%")));
        }
    }
}
