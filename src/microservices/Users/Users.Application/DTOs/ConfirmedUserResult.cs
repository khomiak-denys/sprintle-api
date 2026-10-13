namespace Users.Application.DTOs
{
    /// <summary>
    /// Represents the result of a confirmed email operation.
    /// </summary>
    /// <param name="UserId">The user identifier.</param>
    /// <param name="Email">The confirmed email address.</param>
    public sealed record ConfirmedUserResult(Guid UserId, string Email);
}
