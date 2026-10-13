using DomainFramework.Results;
using FluentAssertions;
using Messaging.Abstractions;
using Messaging.Contracts;
using Users.Application.Auth.Register;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Xunit;

namespace Users.Application.Tests.Auth.Register
{
    public class RegisterCommandHandlerTests
    {
        private readonly FakeAuthService _authService = new();
        private readonly FakeEventPublisher _eventPublisher = new();
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _handler = new RegisterCommandHandler(_authService, _eventPublisher);
        }

        [Fact]
        public async Task Handle_Should_PublishUserRegisteredEvent_When_RegistrationSucceeds()
        {
            // Arrange
            var expectedUserId = Guid.NewGuid();
            var email = "test@example.com";
            var userName = "testuser";
            var password = "Password123!";

            _authService.RegisterResult = Result<AuthTokensResult>.Success(
                new AuthTokensResult("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(15), expectedUserId));

            var command = new RegisterCommand(email, userName, password);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _eventPublisher.PublishedEvents.Should().HaveCount(1);

            var publishedEvent = _eventPublisher.PublishedEvents[0] as UserRegistered;
            publishedEvent.Should().NotBeNull();
            publishedEvent!.Id.Should().Be(expectedUserId);
            publishedEvent.UserName.Should().Be(userName);
            publishedEvent.Email.Should().Be(email);
        }

        private sealed class FakeAuthService : IAuthService
        {
            public Result<AuthTokensResult> RegisterResult { get; set; } = null!;

            public Task<Result<AuthTokensResult>> RegisterAsync(string email, string userName, string password, CancellationToken ct)
                => Task.FromResult(RegisterResult);

            public Task<Result<AuthTokensResult>> LoginAsync(string email, string password, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result<AuthTokensResult>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct)
                => throw new NotImplementedException();

            public Task<Result> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct)
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
