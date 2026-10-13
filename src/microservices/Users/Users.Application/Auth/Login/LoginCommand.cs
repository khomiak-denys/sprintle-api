using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;

namespace Users.Application.Auth.Login
{
    /// <summary>
    /// Command to log in a user with email and password.
    /// </summary>
    /// <param name="Email">The user's email address.</param>
    /// <param name="Password">The user's password.</param>
    public record LoginCommand(string Email, string Password) : ICommand<Result<AuthTokensResult>>;
}
