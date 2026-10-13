namespace Notifications.Application.Abstractions
{
    /// <summary>
    /// Defines a contract for sending email notifications asynchronously.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email message asynchronously to the specified recipient.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The content body of the email (HTML or plain text).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default);
    }
}
