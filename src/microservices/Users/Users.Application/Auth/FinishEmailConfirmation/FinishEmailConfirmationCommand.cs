using DomainFramework.Results;
using Messaging.Abstractions;

namespace Users.Application.Auth.FinishEmailConfirmation
{
    /// <summary>
    /// Command to finish and verify email confirmation with a token.
    /// </summary>
    /// <param name="Token">The email confirmation token.</param>
    /// <param name="UserId">The optional user identifier.</param>
    /// <param name="Email">The optional user email address.</param>
    public record FinishEmailConfirmationCommand(string Token, Guid? UserId = null, string? Email = null) : ICommand<Result>;
}
