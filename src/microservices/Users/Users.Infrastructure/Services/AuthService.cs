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

        public async Task<Result<EmailConfirmationTokenResult>> GenerateEmailConfirmationTokenAsync(string email, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                LogConfirmEmailUserNotFound(email);
                return Result<EmailConfirmationTokenResult>.Failure(new NotFoundError($"User with email '{email}' not found"));
            }

            if (user.EmailConfirmed)
            {
                LogConfirmEmailAlreadyConfirmed(email);
                return Result<EmailConfirmationTokenResult>.Failure(new InvalidArgumentError("Email is already confirmed"));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            return Result<EmailConfirmationTokenResult>.Success(new EmailConfirmationTokenResult(user.Id, user.Email!, token));
        }

        public async Task<Result<ConfirmedUserResult>> FinishEmailConfirmationAsync(string token, Guid? userId, string? email, CancellationToken ct)
        {
            ApplicationUser? user = null;

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                user = await _userManager.FindByIdAsync(userId.Value.ToString());
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user == null)
            {
                var identifier = userId?.ToString() ?? email ?? "unknown";
                LogFinishEmailConfirmationUserNotFound(identifier);
                return Result<ConfirmedUserResult>.Failure(new NotFoundError($"User '{identifier}' not found"));
            }

            if (user.EmailConfirmed)
            {
                LogFinishEmailConfirmationAlreadyConfirmed(user.Id);
                return Result<ConfirmedUserResult>.Failure(new InvalidArgumentError("Email is already confirmed"));
            }

            var identityResult = await _userManager.ConfirmEmailAsync(user, token);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                LogFinishEmailConfirmationFailed(user.Id, errors);
                return Result<ConfirmedUserResult>.Failure(new InvalidArgumentError($"Email confirmation failed: {errors}"));
            }

            LogFinishEmailConfirmationSuccess(user.Id);
            return Result<ConfirmedUserResult>.Success(new ConfirmedUserResult(user.Id, user.Email!));
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

        [LoggerMessage(Level = LogLevel.Warning, Message = "Email confirmation requested for non-existent user with email {Email}")]
        private partial void LogConfirmEmailUserNotFound(string email);

        [LoggerMessage(Level = LogLevel.Information, Message = "Email confirmation requested for already confirmed user with email {Email}")]
        private partial void LogConfirmEmailAlreadyConfirmed(string email);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Finish email confirmation failed: User '{UserIdentifier}' not found")]
        private partial void LogFinishEmailConfirmationUserNotFound(string userIdentifier);

        [LoggerMessage(Level = LogLevel.Information, Message = "Finish email confirmation skipped: Email for user '{UserId}' is already confirmed")]
        private partial void LogFinishEmailConfirmationAlreadyConfirmed(Guid userId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Finish email confirmation failed for user '{UserId}': {Errors}")]
        private partial void LogFinishEmailConfirmationFailed(Guid userId, string errors);

        [LoggerMessage(Level = LogLevel.Information, Message = "Email confirmed successfully for user '{UserId}'")]
        private partial void LogFinishEmailConfirmationSuccess(Guid userId);
    }
}
