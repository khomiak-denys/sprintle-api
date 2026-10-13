using DomainFramework.Errors;
using DomainFramework.Results;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.Auth.FinishEmailConfirmation;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Xunit;

namespace Users.Application.Tests.Auth.FinishEmailConfirmation
{
    public class FinishEmailConfirmationCommandHandlerTests
    {
        private readonly FakeAuthService _authService = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly FinishEmailConfirmationCommandHandler _handler;

        public FinishEmailConfirmationCommandHandlerTests()
        {
            _handler = new FinishEmailConfirmationCommandHandler(_authService, _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_PublishUserEmailConfirmedEvent_When_FinishEmailConfirmationSucceeds()
        {
            // Arrange
            var expectedUserId = Guid.NewGuid();
            var email = "user@example.com";
            var token = "token-123";

            _authService.FinishEmailConfirmationResult = Result<ConfirmedUserResult>.Success(
                new ConfirmedUserResult(expectedUserId, email));

            var command = new FinishEmailConfirmationCommand(token, expectedUserId, email);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _eventPublisher.PublishedEvents.Should().HaveCount(1);

            var publishedEvent = _eventPublisher.PublishedEvents[0] as UserEmailConfirmed;
            publishedEvent.Should().NotBeNull();
            publishedEvent!.Id.Should().Be(expectedUserId);
            publishedEvent.Email.Should().Be(email);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AuthServiceFails()
        {
            // Arrange
            _authService.FinishEmailConfirmationResult = Result<ConfirmedUserResult>.Failure(
                new NotFoundError("User not found"));

            var command = new FinishEmailConfirmationCommand("token-123", Guid.NewGuid(), null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<NotFoundError>();
            _eventPublisher.PublishedEvents.Should().BeEmpty();
        }

        private sealed class FakeAuthService : IAuthService
        {
            public Result<ConfirmedUserResult> FinishEmailConfirmationResult { get; set; } = null!;

            public Task<Result<ConfirmedUserResult>> FinishEmailConfirmationAsync(string token, Guid? userId, string? email, CancellationToken ct)
                => Task.FromResult(FinishEmailConfirmationResult);

            public Task<Result<AuthTokensResult>> RegisterAsync(string email, string userName, string password, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<AuthTokensResult>> LoginAsync(string email, string password, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<AuthTokensResult>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<EmailConfirmationTokenResult>> GenerateEmailConfirmationTokenAsync(string email, CancellationToken ct)
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
