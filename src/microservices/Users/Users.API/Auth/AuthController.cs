using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.ErrorHandling;
using Users.API.Auth.Requests;
using Users.Application.Auth.Login;
using Users.Application.Auth.RefreshToken;
using Users.Application.Auth.Register;
using Users.Application.Auth.RevokeRefreshToken;

namespace Users.API.Auth
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly CookieOptions _cookieOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var result = await _sender.Send(request.ToCommand(), ct);

            if (result.IsSuccess)
            {
                AppendRefreshTokenCookie(result.Value.RefreshToken);
                return Ok(new { AccessToken = result.Value.AccessToken });
            }

            return this.ToActionResult(result.Error);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _sender.Send(request.ToCommand(), ct);

            if (result.IsSuccess)
            {
                AppendRefreshTokenCookie(result.Value.RefreshToken);
                return Ok(new { AccessToken = result.Value.AccessToken });
            }

            return this.ToActionResult(result.Error);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(CancellationToken ct)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized();
            }

            var authorizationHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            var accessToken = authorizationHeader["Bearer ".Length..].Trim();

            var command = new RefreshTokenCommand(accessToken, refreshToken);
            var result = await _sender.Send(command, ct);

            if (result.IsSuccess)
            {
                AppendRefreshTokenCookie(result.Value.RefreshToken);
                return Ok(new { AccessToken = result.Value.AccessToken });
            }

            return this.ToActionResult(result.Error);
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeToken(CancellationToken ct)
        {
            // Extract user id from the authorized user claims
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var command = new RevokeRefreshTokenCommand(userId);
            var result = await _sender.Send(command, ct);

            if (result.IsSuccess)
            {
                ClearRefreshTokenCookie();
                return NoContent();
            }

            return this.ToActionResult(result.Error);
        }

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append("refreshToken", refreshToken, _cookieOptions);
        }

        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", _cookieOptions);
        }
    }
}
