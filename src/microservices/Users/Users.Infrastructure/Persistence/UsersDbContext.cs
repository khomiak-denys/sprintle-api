using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Identity;

namespace Users.Infrastructure.Persistence
{
    public class UsersDbContext : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        ApplicationUserToken>
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

            builder.Entity<ApplicationUserToken>(b =>
            {
                b.Property(t => t.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .IsRequired();

                b.Property(t => t.ExpiresAt)
                    .HasColumnType("timestamp with time zone")
                    .IsRequired();
            });
        }
    }
}
