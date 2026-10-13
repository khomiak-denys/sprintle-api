using DomainFramework.Results;
using Messaging.Abstractions;

namespace Users.Application.Auth.ConfirmEmail
{
    /// <summary>
    /// Command to generate an email confirmation token and send a confirmation email.
    /// </summary>
    /// <param name="Email">The email address of the user to confirm.</param>
    public record ConfirmEmailCommand(string Email) : ICommand<Result>;
}
