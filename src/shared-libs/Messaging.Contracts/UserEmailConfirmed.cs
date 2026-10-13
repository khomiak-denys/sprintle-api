namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a user's email address has been confirmed.
    /// </summary>
    /// <param name="Id">The unique identifier of the user.</param>
    /// <param name="Email">The confirmed email address of the user.</param>
    public sealed record UserEmailConfirmed(Guid Id, string Email);
}
