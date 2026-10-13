using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Infrastructure.Emails;
using NSubstitute;
using Resend;
using Xunit;

namespace Notifications.Infrastructure.Tests.Emails
{
    public class ResendEmailSenderTests
    {
        private readonly IResend _resend;
        private readonly ResendOptions _options;
        private readonly ResendEmailSender _emailSender;

        public ResendEmailSenderTests()
        {
            _resend = Substitute.For<IResend>();
            _options = new ResendOptions
            {
                ApiToken = "re_test_key",
                From = "TaskTracker <noreply@tasktracker.com>"
            };

            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_options);
            _emailSender = new ResendEmailSender(_resend, optionsWrapper, NullLogger<ResendEmailSender>.Instance);
        }

        [Fact]
        public async Task SendAsync_Should_SendEmailSuccessfully_When_ValidParametersProvided()
        {
            // Arrange
            var recipient = "user@example.com";
            var subject = "Welcome to TaskTracker";
            var body = "<p>Welcome!</p>";
            var expectedEmailId = Guid.NewGuid();

            EmailMessage? capturedMessage = null;
            _resend.EmailSendAsync(
                Arg.Do<EmailMessage>(msg => capturedMessage = msg),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ResendResponse<Guid>(expectedEmailId, null!)));

            // Act
            await _emailSender.SendAsync(recipient, subject, body, CancellationToken.None);

            // Assert
            capturedMessage.Should().NotBeNull();
            capturedMessage!.From.Should().NotBeNull();
            capturedMessage!.From!.Email.Should().Be("noreply@tasktracker.com");
            capturedMessage.Subject.Should().Be(subject);
            capturedMessage.HtmlBody.Should().Be(body);
            capturedMessage.To.Should().Contain(addr => addr.Email == recipient);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_Should_ThrowArgumentException_When_RecipientIsEmpty(string recipient)
        {
            // Act
            var act = async () => await _emailSender.SendAsync(recipient, "Subject", "<p>Body</p>", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_Should_ThrowArgumentException_When_SubjectIsEmpty(string subject)
        {
            // Act
            var act = async () => await _emailSender.SendAsync("user@example.com", subject, "<p>Body</p>", CancellationToken.None);

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
        public async Task SendAsync_Should_ThrowInvalidOperationException_When_ResendFails()
        {
            // Arrange
            var ex = new ResendException(HttpStatusCode.BadRequest, ErrorType.ValidationError, "API token invalid", null!);
            _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ResendResponse<Guid>(ex, null!)));

            // Act
            var act = async () => await _emailSender.SendAsync("user@example.com", "Subject", "<p>Body</p>", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Failed to send email to 'user@example.com'*");
        }

        [Fact]
        public void FrontendOptions_Should_StoreEmailConfirmationPageAddress()
        {
            // Arrange & Act
            var options = new FrontendOptions { EmailConfirmationPageAddress = "http://localhost:4200/confirm-email" };

            // Assert
            options.EmailConfirmationPageAddress.Should().Be("http://localhost:4200/confirm-email");
        }
    }
}
