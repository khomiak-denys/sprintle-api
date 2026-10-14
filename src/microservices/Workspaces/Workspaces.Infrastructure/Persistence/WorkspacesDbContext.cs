using Microsoft.EntityFrameworkCore;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tags;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;
using Workspaces.Infrastructure.Persistence.Entities;

namespace Workspaces.Infrastructure.Persistence
{
    public class WorkspacesDbContext : DbContext, IUnitOfWork
    {
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskEventRecord> TaskEvents => Set<TaskEventRecord>();

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<TaskTag> TaskTags => Set<TaskTag>();
        public DbSet<TimeLog> TimeLogs => Set<TimeLog>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();

        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

        public WorkspacesDbContext(DbContextOptions<WorkspacesDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkspacesDbContext).Assembly);
        }
    }
}
