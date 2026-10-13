using EmailTemplates;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;

namespace Notifications.Application.Users
{
    /// <summary>
    /// Handles <see cref="UserEmailConfirmationRequested"/> event to send email confirmation messages to users.
    /// </summary>
    public sealed partial class UserEmailConfirmationRequestedHandler : IEventHandler<UserEmailConfirmationRequested>
    {
        private readonly IEmailSender _emailSender;
        private readonly IOptions<FrontendOptions> _frontendOptions;
        private readonly ILogger<UserEmailConfirmationRequestedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEmailConfirmationRequestedHandler"/> class.
        /// </summary>
        /// <param name="emailSender">The email sender service.</param>
        /// <param name="frontendOptions">The frontend options containing application addresses.</param>
        /// <param name="logger">The logger instance.</param>
        public UserEmailConfirmationRequestedHandler(
            IEmailSender emailSender,
            IOptions<FrontendOptions> frontendOptions,
            ILogger<UserEmailConfirmationRequestedHandler> logger)
        {
            _emailSender = emailSender;
            _frontendOptions = frontendOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleAsync(UserEmailConfirmationRequested @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var baseUrl = _frontendOptions.Value.EmailConfirmationPageAddress;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:4200/confirm-email";
            }

            var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var confirmationUrl = $"{baseUrl}{separator}userId={@event.Id}&email={Uri.EscapeDataString(@event.Email)}&token={Uri.EscapeDataString(@event.Token)}";

            var htmlBody = EmailConfirmationTemplate.Render(confirmationUrl, @event.Email);

            await _emailSender.SendAsync(
                @event.Email,
                EmailConfirmationTemplate.DefaultSubject,
                htmlBody,
                cancellationToken);

            LogEmailConfirmationSent(@event.Id, @event.Email);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Email confirmation sent to user '{UserId}' at '{Email}'.")]
        private partial void LogEmailConfirmationSent(Guid userId, string email);
    }
}
