using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Infrastructure.Emails;
using Xunit;

namespace Notifications.Infrastructure.Tests.Emails
{
    public class SmtpEmailSenderTests
    {
        private readonly SmtpOptions _options;
        private readonly SmtpEmailSender _emailSender;

        public SmtpEmailSenderTests()
        {
            _options = new SmtpOptions
            {
                Host = "localhost",
                Port = 1025,
                From = "Sprintle <noreply@sprintle.local>"
            };

            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_options);
            _emailSender = new SmtpEmailSender(optionsWrapper, NullLogger<SmtpEmailSender>.Instance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_Should_ThrowArgumentException_When_RecipientIsEmpty(string? recipient)
        {
            // Act
            var act = async () => await _emailSender.SendAsync(recipient!, "Subject", "<p>Body</p>", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_Should_ThrowArgumentException_When_SubjectIsEmpty(string? subject)
        {
            // Act
            var act = async () => await _emailSender.SendAsync("user@example.com", subject!, "<p>Body</p>", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendAsync_Should_ThrowArgumentNullException_When_BodyIsNull()
        {
            // Act
            var act = async () => await _emailSender.SendAsync("user@example.com", "Subject", null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void SmtpOptions_Should_HaveDefaultValues_When_Initialized()
        {
            // Arrange & Act
            var options = new SmtpOptions();

            // Assert
            options.Host.Should().Be("localhost");
            options.Port.Should().Be(1025);
            options.From.Should().Be("Sprintle <noreply@sprintle.local>");
            SmtpOptions.SectionName.Should().Be("Smtp");
        }

        [Fact]
        public void SmtpOptions_Should_StoreValues_When_Configured()
        {
            // Arrange & Act
            var options = new SmtpOptions
            {
                Host = "smtp.mailpit.internal",
                Port = 2525,
                From = "Custom <custom@domain.com>"
            };

            // Assert
            options.Host.Should().Be("smtp.mailpit.internal");
            options.Port.Should().Be(2525);
            options.From.Should().Be("Custom <custom@domain.com>");
        }
    }
}
