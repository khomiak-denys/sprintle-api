using Microsoft.EntityFrameworkCore;
using Users.Application.Auth;
using Users.Infrastructure.Identity;
using Users.Infrastructure.Persistence;

namespace Users.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IUserTokenRepository"/>.
    /// </summary>
    internal sealed class UserTokenRepository : IUserTokenRepository
    {
        private const string LoginProvider = "Application";
        private const string TokenName = "RefreshToken";

        private readonly UsersDbContext _context;

        public UserTokenRepository(UsersDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<(string Value, DateTime ExpiresAt)?> GetRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            var token = await _context.UserTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.UserId == userId
                      && t.LoginProvider == LoginProvider
                      && t.Name == TokenName,
                    cancellationToken);

            if (token is null || token.Value is null)
                return null;

            return (token.Value, token.ExpiresAt);
        }

        /// <inheritdoc/>
        public async Task UpsertRefreshTokenAsync(
            Guid userId,
            string value,
            DateTime createdAt,
            DateTime expiresAt,
            CancellationToken cancellationToken)
        {
            var existing = await _context.UserTokens
                .FirstOrDefaultAsync(
                    t => t.UserId == userId
                      && t.LoginProvider == LoginProvider
                      && t.Name == TokenName,
                    cancellationToken);

            if (existing is null)
            {
                _context.UserTokens.Add(new ApplicationUserToken
                {
                    UserId = userId,
                    LoginProvider = LoginProvider,
                    Name = TokenName,
                    Value = value,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                });
            }
            else
            {
                existing.Value = value;
                existing.CreatedAt = createdAt;
                existing.ExpiresAt = expiresAt;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RemoveRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            var token = await _context.UserTokens
                .FirstOrDefaultAsync(
                    t => t.UserId == userId
                      && t.LoginProvider == LoginProvider
                      && t.Name == TokenName,
                    cancellationToken);

            if (token is not null)
            {
                _context.UserTokens.Remove(token);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
