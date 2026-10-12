using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Infrastructure.Identity;
using Users.Infrastructure.Persistence;
using Users.Infrastructure.Persistence.Configurations;

namespace Users.Infrastructure.Services
{
    internal sealed class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UsersDbContext _dbContext;

        public UserService(UserManager<ApplicationUser> userManager, UsersDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<Result<UserProfileResult>> GetByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserProfileResult>.Failure(new NotFoundError("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var dto = new UserProfileResult(
                user.Id,
                user.Email!,
                user.UserName!,
                user.FullName,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnd,
                user.LockoutEnabled,
                user.AccessFailedCount,
                roles.ToList());
            return Result<UserProfileResult>.Success(dto);
        }

        public async Task<Result<UserContactInfoResult>> GetContactInfoByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserContactInfoResult>.Failure(new NotFoundError("User not found"));
            }

            var dto = new UserContactInfoResult(
                user.Id,
                user.Email!,
                user.UserName!,
                user.FullName);

            return Result<UserContactInfoResult>.Success(dto);
        }

        public async Task<Result<PaginationResult<UserContactInfoResult>>> GetContactInfoBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            string? search,
            bool? isWorkspaceMember,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var effectivePage = page < 1 ? 1 : page;
            var effectivePageSize = pageSize < 1 ? 10 : pageSize;

            IQueryable<ApplicationUser> query;

            if (isWorkspaceMember == false)
            {
                query = _dbContext.Users.AsNoTracking();

                if (userIds != null && userIds.Count > 0)
                {
                    var distinctIds = userIds.Distinct().ToList();
                    query = query.Where(u => !distinctIds.Contains(u.Id));
                }

                var adminRoleId = RoleConfiguration.AdminRoleId;
                query = query.Where(u => !_dbContext.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));
            }
            else
            {
                if (userIds == null || userIds.Count == 0)
                {
                    var emptyResult = PaginationResult<UserContactInfoResult>.Create(
                        Array.Empty<UserContactInfoResult>(),
                        effectivePage,
                        effectivePageSize,
                        0);
                    return Result<PaginationResult<UserContactInfoResult>>.Success(emptyResult);
                }

                var distinctIds = userIds.Distinct().ToList();
                query = _dbContext.Users
                    .AsNoTracking()
                    .Where(u => distinctIds.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.UserName != null && EF.Functions.ILike(u.UserName, $"%{term}%")) ||
                    (u.FullName != null && EF.Functions.ILike(u.FullName, $"%{term}%")) ||
                    (u.Email != null && EF.Functions.ILike(u.Email, $"%{term}%")));
            }

            var totalCount = await query.CountAsync(ct);

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .Select(u => new UserContactInfoResult(
                    u.Id,
                    u.Email ?? string.Empty,
                    u.UserName ?? string.Empty,
                    u.FullName))
                .ToListAsync(ct);

            var result = PaginationResult<UserContactInfoResult>.Create(users, effectivePage, effectivePageSize, totalCount);
            return Result<PaginationResult<UserContactInfoResult>>.Success(result);
        }

        public async Task<Result<PaginationResult<UserResult>>> GetAllAsync(int page, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.Users.AsNoTracking();
            var totalCount = await query.CountAsync(ct);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserResult(u.Id, u.Email!, u.UserName!, u.FullName))
                .ToListAsync(ct);

            var result = PaginationResult<UserResult>.Create(users, page, pageSize, totalCount);
            return Result<PaginationResult<UserResult>>.Success(result);
        }

        public async Task<Result> UpdateProfileAsync(Guid userId, string? fullName, string? userName, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure(new NotFoundError("User not found"));
            }

            if (userName != null)
            {
                user.UserName = userName.Trim();
            }
            if (fullName != null)
            {
                user.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Result.Failure(new InvalidArgumentError(string.Join(", ", result.Errors.Select(e => e.Description))));
            }

            return Result.Success();
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure(new NotFoundError("User not found"));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                return Result.Failure(new InvalidArgumentError(string.Join(", ", result.Errors.Select(e => e.Description))));
            }

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure(new NotFoundError("User not found"));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Result.Failure(new InvalidArgumentError(string.Join(", ", result.Errors.Select(e => e.Description))));
            }

            return Result.Success();
        }
    }
}
