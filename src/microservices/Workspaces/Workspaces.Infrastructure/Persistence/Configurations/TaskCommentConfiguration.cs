using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workspaces.Domain.Tasks;

namespace Workspaces.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core entity type configuration for <see cref="TaskComment"/>.
    /// </summary>
    public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
    {
        /// <summary>
        /// Configures the entity of type <see cref="TaskComment"/>.
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity type.</param>
        public void Configure(EntityTypeBuilder<TaskComment> builder)
        {
            builder.ToTable("TaskComments");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.TaskId).IsRequired();
            builder.Property(c => c.AuthorId).IsRequired();

            builder.Property(c => c.Content)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt);

            builder.HasIndex(c => new { c.TaskId, c.CreatedAt })
                .HasDatabaseName("IX_TaskComments_TaskId_CreatedAt");
        }
    }
}
