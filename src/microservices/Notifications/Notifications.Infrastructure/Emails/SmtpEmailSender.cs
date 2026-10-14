using System;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Emails
{
    /// <summary>
    /// Implements <see cref="IEmailSender"/> by sending email messages via local SMTP (e.g. Mailpit).
    /// </summary>
    public sealed partial class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<SmtpOptions> _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailSender"/> class.
        /// </summary>
        /// <param name="options">The configured SMTP options.</param>
        /// <param name="logger">The logger instance.</param>
        public SmtpEmailSender(
            IOptions<SmtpOptions> options,
            ILogger<SmtpEmailSender> logger)
        {
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

            var host = string.IsNullOrWhiteSpace(_options.Value.Host) ? "localhost" : _options.Value.Host;
            var port = _options.Value.Port <= 0 ? 1025 : _options.Value.Port;
            var from = string.IsNullOrWhiteSpace(_options.Value.From) ? "TaskTracker <noreply@tasktracker.local>" : _options.Value.From;

            MailAddress fromAddress;
            try
            {
                fromAddress = new MailAddress(from);
            }
            catch (FormatException)
            {
                fromAddress = new MailAddress("noreply@tasktracker.local", "TaskTracker");
            }

            using var message = new MailMessage
            {
                From = fromAddress,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            LogSendingEmail(to, subject, host, port);

            using var client = new SmtpClient(host, port);
            await client.SendMailAsync(message, cancellationToken);

            LogEmailSentSuccessfully(to, subject);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Sending email to '{To}' with subject '{Subject}' via SMTP '{Host}:{Port}'.")]
        private partial void LogSendingEmail(string to, string subject, string host, int port);

        [LoggerMessage(Level = LogLevel.Information, Message = "Email to '{To}' with subject '{Subject}' sent successfully via SMTP.")]
        private partial void LogEmailSentSuccessfully(string to, string subject);
    }
}
