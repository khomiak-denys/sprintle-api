using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;

namespace Users.Application.Auth.Register
{
    /// <summary>
    /// Command to register a new user account.
    /// </summary>
    /// <param name="Email">The user's email address.</param>
    /// <param name="UserName">The user's username.</param>
    /// <param name="Password">The user's password.</param>
    public record RegisterCommand(string Email, string UserName, string Password) : ICommand<Result<AuthTokensResult>>;
}
