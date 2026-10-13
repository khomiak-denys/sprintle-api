using DomainFramework.Errors;
using DomainFramework.Results;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.Auth.ConfirmEmail;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Xunit;

namespace Users.Application.Tests.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandHandlerTests
    {
        private readonly FakeAuthService _authService = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly ConfirmEmailCommandHandler _handler;

        public ConfirmEmailCommandHandlerTests()
        {
            _handler = new ConfirmEmailCommandHandler(_authService, _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_PublishUserEmailConfirmationRequestedEvent_When_TokenGenerationSucceeds()
        {
            // Arrange
            var expectedUserId = Guid.NewGuid();
            var email = "user@example.com";
            var token = "confirmation-token-xyz";

            _authService.GenerateTokenResult = Result<EmailConfirmationTokenResult>.Success(
                new EmailConfirmationTokenResult(expectedUserId, email, token));

            var command = new ConfirmEmailCommand(email);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _eventPublisher.PublishedEvents.Should().HaveCount(1);

            var publishedEvent = _eventPublisher.PublishedEvents[0] as UserEmailConfirmationRequested;
            publishedEvent.Should().NotBeNull();
            publishedEvent!.Id.Should().Be(expectedUserId);
            publishedEvent.Email.Should().Be(email);
            publishedEvent.Token.Should().Be(token);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AuthServiceFails()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _authService.GenerateTokenResult = Result<EmailConfirmationTokenResult>.Failure(
                new NotFoundError("User not found"));

            var command = new ConfirmEmailCommand(email);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<NotFoundError>();
            _eventPublisher.PublishedEvents.Should().BeEmpty();
        }

        private sealed class FakeAuthService : IAuthService
        {
            public Result<EmailConfirmationTokenResult> GenerateTokenResult { get; set; } = null!;

            public Task<Result<EmailConfirmationTokenResult>> GenerateEmailConfirmationTokenAsync(string email, CancellationToken ct)
                => Task.FromResult(GenerateTokenResult);

            public Task<Result<AuthTokensResult>> RegisterAsync(string email, string userName, string password, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<AuthTokensResult>> LoginAsync(string email, string password, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<AuthTokensResult>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<ConfirmedUserResult>> FinishEmailConfirmationAsync(string token, Guid? userId, string? email, CancellationToken ct)
                => throw new NotImplementedException();
        }

        private sealed class FakeEventPublisher : IEventPublisher
        {
            public List<object> PublishedEvents { get; } = [];

            public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
                where TEvent : class
            {
                PublishedEvents.Add(@event);
                return Task.CompletedTask;
            }
        }
    }
}
