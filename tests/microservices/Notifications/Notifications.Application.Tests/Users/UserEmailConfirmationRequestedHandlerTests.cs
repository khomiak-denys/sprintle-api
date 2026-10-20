using EmailTemplates;
using FluentAssertions;
using Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Application.Users;
using NSubstitute;
using Xunit;

namespace Notifications.Application.Tests.Users
{
    public class UserEmailConfirmationRequestedHandlerTests
    {
        private readonly IEmailSender _emailSender;
        private readonly FrontendOptions _frontendOptions;
        private readonly UserEmailConfirmationRequestedHandler _handler;

        public UserEmailConfirmationRequestedHandlerTests()
        {
            _emailSender = Substitute.For<IEmailSender>();
            _frontendOptions = new FrontendOptions
            {
                EmailConfirmationPageAddress = "http://localhost:4200/confirm-email"
            };

            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_frontendOptions);
            _handler = new UserEmailConfirmationRequestedHandler(
                _emailSender,
                optionsWrapper,
                NullLogger<UserEmailConfirmationRequestedHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_Should_SendEmailSuccessfully_When_ValidEventProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "testuser@example.com";
            var token = "raw-token+value==";
            var @event = new UserEmailConfirmationRequested(userId, email, token);

            string? capturedTo = null;
            string? capturedSubject = null;
            string? capturedBody = null;

            _emailSender.SendAsync(
                Arg.Do<string>(to => capturedTo = to),
                Arg.Do<string>(sub => capturedSubject = sub),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            await _emailSender.Received(1).SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            capturedTo.Should().Be(email);
            capturedSubject.Should().Be(EmailConfirmationTemplate.DefaultSubject);
            capturedBody.Should().NotBeNullOrWhiteSpace();
            capturedBody.Should().Contain(userId.ToString());
            capturedBody.Should().Contain(Uri.EscapeDataString(token));
            capturedBody.Should().Contain("Confirm Email");
        }

        [Fact]
        public async Task HandleAsync_Should_UseCustomPageAddress_When_ProvidedInOptions()
        {
            // Arrange
            _frontendOptions.EmailConfirmationPageAddress = "https://app.sprintle.com/auth/verify";
            var userId = Guid.NewGuid();
            var email = "custom@example.com";
            var token = "custom-token-123";
            var @event = new UserEmailConfirmationRequested(userId, email, token);

            string? capturedBody = null;
            _emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("https://app.sprintle.com/auth/verify?userId=");
        }

        [Fact]
        public async Task HandleAsync_Should_HandleExistingQueryParameters_When_BaseUrlContainsQuestionMark()
        {
            // Arrange
            _frontendOptions.EmailConfirmationPageAddress = "https://app.sprintle.com/auth/verify?source=email";
            var userId = Guid.NewGuid();
            var email = "query@example.com";
            var token = "query-token";
            var @event = new UserEmailConfirmationRequested(userId, email, token);

            string? capturedBody = null;
            _emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("https://app.sprintle.com/auth/verify?source=email&amp;userId=");
        }

        [Fact]
        public async Task HandleAsync_Should_UseDefaultFallbackAddress_When_OptionsAddressIsEmpty()
        {
            // Arrange
            _frontendOptions.EmailConfirmationPageAddress = "";
            var userId = Guid.NewGuid();
            var email = "fallback@example.com";
            var token = "fallback-token";
            var @event = new UserEmailConfirmationRequested(userId, email, token);

            string? capturedBody = null;
            _emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(@event, CancellationToken.None);

            // Assert
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("http://localhost:4200/confirm-email?userId=");
        }

        [Fact]
        public async Task HandleAsync_Should_ThrowArgumentNullException_When_EventIsNull()
        {
            // Act
            var act = async () => await _handler.HandleAsync(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
