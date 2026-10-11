using DomainFramework.Errors;
using DomainFramework.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDefaults.Authentification;
using Users.Application.Auth;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Infrastructure.Identity;

namespace Users.Infrastructure.Services
{
    internal sealed partial class AuthService : IAuthService
    {
        private const int RefreshTokenValidityDays = 7;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtTokenService _tokenService;
        private readonly IUserTokenRepository _tokenRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtOptions> jwtOptions,
            IUserTokenRepository tokenRepository,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = new JwtTokenService(jwtOptions);
            _tokenRepository = tokenRepository;
            _logger = logger;
        }

        public async Task<Result<AuthTokensResult>> RegisterAsync(string email, string userName, string password, CancellationToken ct)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                LogRegistrationUserAlreadyExists(email);
                return Result<AuthTokensResult>.Failure(new AlreadyExistsError("User with this email already exists"));
            }

            var user = new ApplicationUser
            {
                Email = email,
                UserName = userName
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                LogRegistrationFailed(email, errors);
                return Result<AuthTokensResult>.Failure(new InvalidArgumentError(errors));
            }

            return await GenerateTokensAndUpdateUserAsync(user, ct);
        }

        public async Task<Result<AuthTokensResult>> LoginAsync(string email, string password, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                LogLoginUserNotFound(email);
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Invalid email or password"));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded)
            {
                LogLoginInvalidPassword(email);
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Invalid email or password"));
            }

            return await GenerateTokensAndUpdateUserAsync(user, ct);
        }

        public async Task<Result<AuthTokensResult>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                LogRefreshTokenInvalidPrincipals();
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Invalid token principals"));
            }

            var userName = principal.Identity?.Name;
            if (userName == null)
            {
                LogRefreshTokenUsernameNotFound();
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Username in token not found"));
            }

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                LogRefreshTokenUserNotFound(userName);
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("User not found"));
            }

            var stored = await _tokenRepository.GetRefreshTokenAsync(user.Id, ct);
            if (stored is null)
            {
                LogRefreshTokenStoredTokenNotFound(userName);
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Token not found"));
            }

            if (stored.Value.Value != refreshToken || stored.Value.ExpiresAt <= DateTime.UtcNow)
            {
                LogRefreshTokenInvalidOrExpired(userName);
                return Result<AuthTokensResult>.Failure(new UnauthorizedError("Invalid refresh token"));
            }

            return await GenerateTokensAndUpdateUserAsync(user, ct);
        }

        public async Task<Result> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                LogRevokeRefreshTokenUserNotFound(userId);
                return Result.Failure(new NotFoundError("User not found"));
            }

            await _tokenRepository.RemoveRefreshTokenAsync(userId, ct);

            return Result.Success();
        }

        private async Task<Result<AuthTokensResult>> GenerateTokensAndUpdateUserAsync(ApplicationUser user, CancellationToken ct)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var tokens = _tokenService.GenerateTokens(user, roles);

            var now = DateTime.UtcNow;
            await _tokenRepository.UpsertRefreshTokenAsync(
                user.Id,
                tokens.RefreshToken,
                now,
                now.AddDays(RefreshTokenValidityDays),
                ct);

            return Result<AuthTokensResult>.Success(tokens);
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Registration failed: User with email {Email} already exists")]
        private partial void LogRegistrationUserAlreadyExists(string email);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Registration failed for user {Email}: {Errors}")]
        private partial void LogRegistrationFailed(string email, string errors);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed: User with email {Email} not found")]
        private partial void LogLoginUserNotFound(string email);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed: Invalid password for user {Email}")]
        private partial void LogLoginInvalidPassword(string email);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token failed: Invalid token principals")]
        private partial void LogRefreshTokenInvalidPrincipals();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token failed: Username in token not found")]
        private partial void LogRefreshTokenUsernameNotFound();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token failed: User {UserName} not found")]
        private partial void LogRefreshTokenUserNotFound(string userName);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token failed: Stored token for user {UserName} not found")]
        private partial void LogRefreshTokenStoredTokenNotFound(string userName);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token failed: Invalid or expired refresh token for user {UserName}")]
        private partial void LogRefreshTokenInvalidOrExpired(string userName);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Revoke refresh token failed: User {UserId} not found")]
        private partial void LogRevokeRefreshTokenUserNotFound(Guid userId);
    }
}
