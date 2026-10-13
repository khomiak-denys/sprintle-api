using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Resend;

namespace Notifications.Infrastructure.Emails
{
    /// <summary>
    /// Implements <see cref="IEmailSender"/> by sending email messages using the Resend service.
    /// </summary>
    public sealed partial class ResendEmailSender : IEmailSender
    {
        private readonly IResend _resend;
        private readonly IOptions<ResendOptions> _options;
        private readonly ILogger<ResendEmailSender> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResendEmailSender"/> class.
        /// </summary>
        /// <param name="resend">The Resend client service.</param>
        /// <param name="options">The configured Resend options.</param>
        /// <param name="logger">The logger instance.</param>
        public ResendEmailSender(
            IResend resend,
            IOptions<ResendOptions> options,
            ILogger<ResendEmailSender> logger)
        {
            _resend = resend;
            _options = options;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentNullException.ThrowIfNull(body);

            var sender = _options.Value.From;

            var message = new EmailMessage
            {
                From = sender,
                Subject = subject,
                HtmlBody = body
            };
            message.To.Add(to);

            LogSendingEmail(to, subject);

            var response = await _resend.EmailSendAsync(message, cancellationToken);

            if (!response.Success)
            {
                var errorMessage = response.Exception?.Message ?? "Unknown Resend error";
                LogEmailSendFailed(to, errorMessage);
                throw new InvalidOperationException($"Failed to send email to '{to}': {errorMessage}", response.Exception);
            }

            LogEmailSentSuccessfully(to, response.Content);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Sending email to '{To}' with subject '{Subject}'.")]
        private partial void LogSendingEmail(string to, string subject);

        [LoggerMessage(Level = LogLevel.Information, Message = "Email to '{To}' sent successfully. Resend EmailId: '{EmailId}'.")]
        private partial void LogEmailSentSuccessfully(string to, Guid emailId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to '{To}'. Error: '{Error}'.")]
        private partial void LogEmailSendFailed(string to, string error);
    }
}
