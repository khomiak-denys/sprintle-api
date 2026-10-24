using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core entity type configuration for <see cref="Workspace"/>.
    /// </summary>
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        /// <summary>
        /// Configures the entity of type <see cref="Workspace"/>.
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity type.</param>
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            builder.ToTable("Workspaces");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(w => w.OwnerId)
                .IsRequired();

            builder.Property(w => w.Description)
                .HasMaxLength(2000);

            // Incremented only via an atomic SQL UPDATE in WorkspaceRepository.ReserveNextTaskNumberAsync.
            // Ignored on update so a stale tracked instance can never overwrite the counter.
            builder.Property(w => w.LastTaskNumber)
                .IsRequired()
                .HasDefaultValue(0)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            builder.Property(w => w.CreatedAt)
                .IsRequired();

            builder.Property(w => w.UpdatedAt);

            builder.HasMany(w => w.Members)
                .WithOne(m => m.Workspace)
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
