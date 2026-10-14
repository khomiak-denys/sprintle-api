using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workspaces.Infrastructure.Persistence.Entities;

namespace Workspaces.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core entity type configuration for <see cref="TaskEventRecord"/>.
    /// </summary>
    public class TaskEventRecordConfiguration : IEntityTypeConfiguration<TaskEventRecord>
    {
        /// <summary>
        /// Configures the entity of type <see cref="TaskEventRecord"/>.
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity type.</param>
        public void Configure(EntityTypeBuilder<TaskEventRecord> builder)
        {
            builder.ToTable("TaskEvents");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.TaskId)
                .IsRequired();

            builder.Property(e => e.Version)
                .IsRequired();

            builder.Property(e => e.EventType)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(e => e.Payload)
                .IsRequired();

            builder.Property(e => e.OccurredAt)
                .IsRequired();

            // Stream query index
            builder.HasIndex(e => e.TaskId);

            // Optimistic concurrency index: each event version per task stream is strictly unique
            builder.HasIndex(e => new { e.TaskId, e.Version })
                .IsUnique();
        }
    }
}
