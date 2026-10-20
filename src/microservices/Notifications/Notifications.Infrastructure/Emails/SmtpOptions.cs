namespace Notifications.Infrastructure.Emails
{
    /// <summary>
    /// Configuration options for local SMTP email delivery (e.g., Mailpit).
    /// </summary>
    public sealed class SmtpOptions
    {
        /// <summary>
        /// The configuration section name in application settings.
        /// </summary>
        public const string SectionName = "Smtp";

        /// <summary>
        /// Gets or sets the SMTP server host. Defaults to "localhost".
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the SMTP server port. Defaults to 1025.
        /// </summary>
        public int Port { get; set; } = 1025;

        /// <summary>
        /// Gets or sets the default sender email address.
        /// </summary>
        public string From { get; set; } = "Sprintle <noreply@sprintle.local>";
    }
}
