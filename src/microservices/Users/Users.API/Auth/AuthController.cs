using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;
using Users.API.Auth.Requests;
using Users.Application.Auth.ConfirmEmail;
using Users.Application.Auth.FinishEmailConfirmation;
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

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        [SwaggerOperation(Summary = "Requests an email confirmation token and dispatches confirmation email event")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest? request, CancellationToken ct)
        {
            var email = request?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid argument",
                    Detail = "Email is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new ConfirmEmailCommand(email);
            var result = await _sender.Send(command, ct);

            return result.Match(Ok(), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Finishes email confirmation by verifying the provided token against the specified user.
        /// </summary>
        /// <param name="request">The finish email confirmation request payload.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An HTTP 200 OK response if successful, or a problem details error response.</returns>
        [AllowAnonymous]
        [HttpPost("confirm-email/finish")]
        [SwaggerOperation(Summary = "Finishes email confirmation by verifying the token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FinishEmailConfirmation([FromBody] FinishEmailConfirmationRequest request, CancellationToken ct)
        {
            var userId = request.UserId;
            var email = request.Email;

            if (!userId.HasValue && string.IsNullOrWhiteSpace(email))
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdString, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
                else
                {
                    email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                }
            }

            var command = request.ToCommand(userId, email);
            var result = await _sender.Send(command, ct);

            return result.Match(Ok(), error => this.ToActionResult(error));
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
