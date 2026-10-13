using Users.Application.Auth.FinishEmailConfirmation;

namespace Users.API.Auth.Requests
{
    /// <summary>
    /// Request payload for finishing email confirmation with a token.
    /// </summary>
    /// <param name="Token">The email confirmation token.</param>
    /// <param name="UserId">The optional user identifier.</param>
    /// <param name="Email">The optional user email address.</param>
    public record FinishEmailConfirmationRequest(string Token, Guid? UserId = null, string? Email = null)
    {
        /// <summary>
        /// Converts to <see cref="FinishEmailConfirmationCommand"/>.
        /// </summary>
        /// <param name="resolvedUserId">The optional resolved user identifier from claims or request.</param>
        /// <param name="resolvedEmail">The optional resolved user email address from claims or request.</param>
        /// <returns>A new <see cref="FinishEmailConfirmationCommand"/> instance.</returns>
        public FinishEmailConfirmationCommand ToCommand(Guid? resolvedUserId = null, string? resolvedEmail = null)
        {
            return new FinishEmailConfirmationCommand(Token, resolvedUserId ?? UserId, resolvedEmail ?? Email);
        }
    }
}
