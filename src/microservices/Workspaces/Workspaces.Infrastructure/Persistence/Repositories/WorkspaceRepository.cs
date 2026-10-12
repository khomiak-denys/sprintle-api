using DomainFramework;
using Microsoft.EntityFrameworkCore;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IWorkspaceRepository"/>.
    /// </summary>
    internal sealed class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly WorkspacesDbContext _context;

        public WorkspaceRepository(WorkspacesDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PaginationResult<Workspace>> GetByMemberAsync(Guid userId, string? name, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Workspaces
                .Include(w => w.Members)
                .AsNoTracking()
                .Where(w => w.Members.Any(m => m.UserId == userId));

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(w => EF.Functions.ILike(w.Name, $"%{name}%"));
            }

            query = query.OrderByDescending(w => w.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginationResult<Workspace>.Create(items, page, pageSize, totalCount);
        }

        /// <inheritdoc/>
        public async Task<PaginationResult<Workspace>> GetAllAsync(string? name, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Workspaces
                .Include(w => w.Members)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(w => EF.Functions.ILike(w.Name, $"%{name}%"));
            }

            query = query.OrderByDescending(w => w.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginationResult<Workspace>.Create(items, page, pageSize, totalCount);
        }

        /// <inheritdoc/>
        public async Task AddAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            await _context.Workspaces.AddAsync(workspace, cancellationToken);
        }

        /// <inheritdoc/>
        public Task RemoveAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            _context.Workspaces.Remove(workspace);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task AddMemberAsync(WorkspaceMember member, CancellationToken cancellationToken)
        {
            await _context.WorkspaceMembers.AddAsync(member, cancellationToken);
        }

        /// <inheritdoc/>
        public Task RemoveMemberAsync(WorkspaceMember member, CancellationToken cancellationToken)
        {
            _context.WorkspaceMembers.Remove(member);
            return Task.CompletedTask;
        }
    }
}
