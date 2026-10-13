using System;

namespace Messaging.Contracts
{
    /// <summary>
    /// Event published when a new user has successfully registered.
    /// </summary>
    /// <param name="Id">The unique identifier of the registered user.</param>
    /// <param name="UserName">The username of the registered user.</param>
    /// <param name="Email">The email address of the registered user.</param>
    public sealed record UserRegistered(Guid Id, string UserName, string Email);
}
