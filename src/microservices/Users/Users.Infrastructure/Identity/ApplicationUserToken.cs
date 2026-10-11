using Microsoft.AspNetCore.Identity;

namespace Users.Infrastructure.Identity
{
    /// <summary>
    /// Extended Identity user token that stores metadata for the refresh token.
    /// </summary>
    public class ApplicationUserToken : IdentityUserToken<Guid>
    {
        /// <summary>Gets or sets the UTC timestamp when the token was issued.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Gets or sets the UTC timestamp when the token expires.</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
