using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core entity type configuration for <see cref="Notification"/>.
    /// </summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        /// <summary>
        /// Configures the entity of type <see cref="Notification"/>.
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity type.</param>
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.UserId)
                .IsRequired();

            builder.Property(n => n.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(n => n.Username)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(n => n.NotificationType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.Enabled)
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.UpdatedAt);

            builder.HasIndex(n => n.UserId);

            builder.HasIndex(n => new { n.UserId, n.NotificationType })
                .IsUnique();
        }
    }
}
