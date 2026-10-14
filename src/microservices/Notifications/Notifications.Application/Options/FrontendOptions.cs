namespace Notifications.Application.Options
{
    /// <summary>
    /// Configuration options for frontend application addresses and page URLs.
    /// </summary>
    public sealed class FrontendOptions
    {
        /// <summary>
        /// The configuration section name in application settings.
        /// </summary>
        public const string SectionName = "Frontend";

        /// <summary>
        /// Gets or sets the email confirmation page address of the frontend application.
        /// </summary>
        public string EmailConfirmationPageAddress { get; set; } = string.Empty;
    }
}
