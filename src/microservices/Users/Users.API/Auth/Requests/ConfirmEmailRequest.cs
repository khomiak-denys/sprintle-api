using Users.Application.Auth.ConfirmEmail;

namespace Users.API.Auth.Requests
{
    /// <summary>
    /// Request payload for requesting an email confirmation token.
    /// </summary>
    /// <param name="Email">The email address of the user to confirm.</param>
    public record ConfirmEmailRequest(string? Email = null)
    {
        /// <summary>
        /// Converts to <see cref="ConfirmEmailCommand"/> with the resolved email address.
        /// </summary>
        /// <param name="resolvedEmail">The resolved email address to use for the command.</param>
        /// <returns>A new <see cref="ConfirmEmailCommand"/> instance.</returns>
        public ConfirmEmailCommand ToCommand(string resolvedEmail)
        {
            return new ConfirmEmailCommand(resolvedEmail);
        }
    }
}
