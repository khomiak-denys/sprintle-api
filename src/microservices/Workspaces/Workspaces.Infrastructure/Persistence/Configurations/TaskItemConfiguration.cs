using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core entity type configuration for <see cref="TaskItem"/>.
    /// </summary>
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        /// <summary>
        /// Configures the entity of type <see cref="TaskItem"/>.
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity type.</param>
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.WorkspaceId)
                .IsRequired();

            builder.HasIndex(t => t.WorkspaceId);

            // Backend-generated, per-workspace formatted identifier (e.g. DMS-730). Never updated after insert.
            builder.Property(t => t.Number)
                .HasMaxLength(50)
                .IsRequired()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            builder.HasIndex(t => new { t.WorkspaceId, t.Number })
                .IsUnique();

            builder.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(2000);

            builder.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.AssigneeId);
            builder.Property(t => t.CreatedById).IsRequired();
            builder.Property(t => t.Deadline);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.UpdatedAt);
            builder.Property(t => t.Version).IsRequired().HasDefaultValue(0);

            builder.Ignore(t => t.UncommittedEvents);

            builder.HasMany(t => t.TimeLogs)
                .WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.TaskTags)
                .WithOne(tt => tt.Task)
                .HasForeignKey(tt => tt.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Comments)
                .WithOne(c => c.Task)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
