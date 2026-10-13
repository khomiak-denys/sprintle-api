namespace Users.Application.DTOs
{
    /// <summary>
    /// Represents the result of generating an email confirmation token.
    /// </summary>
    /// <param name="UserId">The user identifier.</param>
    /// <param name="Email">The user email.</param>
    /// <param name="Token">The confirmation token.</param>
    public sealed record EmailConfirmationTokenResult(Guid UserId, string Email, string Token);
}
