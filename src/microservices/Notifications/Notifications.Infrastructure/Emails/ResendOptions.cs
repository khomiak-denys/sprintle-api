namespace Notifications.Infrastructure.Emails
{
    /// <summary>
    /// Configuration options for the Resend email service.
    /// </summary>
    public sealed class ResendOptions
    {
        /// <summary>
        /// The configuration section name in application settings.
        /// </summary>
        public const string SectionName = "Resend";

        /// <summary>
        /// Gets or sets the Resend API token.
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default sender email address (e.g. "Sprintle &lt;onboarding@resend.dev&gt;").
        /// </summary>
        public string From { get; set; } = string.Empty;
    }
}
