namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when email confirmation is requested for a user.
    /// </summary>
    /// <param name="Id">The unique identifier of the user.</param>
    /// <param name="Email">The email address of the user.</param>
    /// <param name="Token">The email confirmation token.</param>
    public sealed record UserEmailConfirmationRequested(Guid Id, string Email, string Token);
}
