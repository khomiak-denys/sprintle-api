using System.Text;
using System.Text.Json;
using FluentAssertions;
using MessageBus.Consumer;
using MessageBus.Convention;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Notifications.Infrastructure.Tests.Consumers
{
    public class EventDispatcherTests
    {
        private readonly EventDispatcher _dispatcher;

        public EventDispatcherTests()
        {
            var routingConvention = new RabbitMqRoutingConvention();
            _dispatcher = new EventDispatcher(routingConvention);
        }

        [Fact]
        public async Task DispatchAsync_Should_InvokeHandler_When_RoutingKeyIsMapped()
        {
            // Arrange
            _dispatcher.Map<UserRegistered>();

            var handledEvent = (UserRegistered?)null;
            var handlerMock = new TestUserRegisteredHandler(evt => handledEvent = evt);

            var services = new ServiceCollection();
            services.AddSingleton<IEventHandler<UserRegistered>>(handlerMock);
            using var serviceProvider = services.BuildServiceProvider();

            var expectedEvent = new UserRegistered(Guid.NewGuid(), "testuser", "test@example.com");
            var body = JsonSerializer.SerializeToUtf8Bytes(expectedEvent);

            // Act
            await _dispatcher.DispatchAsync("user.registered", serviceProvider, body, CancellationToken.None);

            // Assert
            handledEvent.Should().NotBeNull();
            handledEvent!.Id.Should().Be(expectedEvent.Id);
            handledEvent.UserName.Should().Be(expectedEvent.UserName);
            handledEvent.Email.Should().Be(expectedEvent.Email);
        }

        [Fact]
        public async Task DispatchAsync_Should_ThrowInvalidOperationException_When_RoutingKeyIsUnknown()
        {
            // Arrange
            var services = new ServiceCollection();
            using var serviceProvider = services.BuildServiceProvider();
            var body = Encoding.UTF8.GetBytes("{}");

            // Act
            var act = async () => await _dispatcher.DispatchAsync("unknown.key", serviceProvider, body, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*unknown.key*");
        }

        [Fact]
        public async Task DispatchAsync_Should_ThrowJsonException_When_DeserializationProducesNull()
        {
            // Arrange
            _dispatcher.Map<UserRegistered>();

            var services = new ServiceCollection();
            services.AddSingleton<IEventHandler<UserRegistered>>(new TestUserRegisteredHandler(_ => { }));
            using var serviceProvider = services.BuildServiceProvider();
            var body = Encoding.UTF8.GetBytes("null");

            // Act
            var act = async () => await _dispatcher.DispatchAsync("user.registered", serviceProvider, body, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<JsonException>();
        }

        private sealed class TestUserRegisteredHandler : IEventHandler<UserRegistered>
        {
            private readonly Action<UserRegistered> _callback;

            public TestUserRegisteredHandler(Action<UserRegistered> callback)
            {
                _callback = callback;
            }

            public Task HandleAsync(UserRegistered @event, CancellationToken cancellationToken = default)
            {
                _callback(@event);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task DispatchAsync_Should_InvokeHandler_When_UserEmailConfirmationRequestedIsMapped()
        {
            // Arrange
            _dispatcher.Map<UserEmailConfirmationRequested>();

            var handledEvent = (UserEmailConfirmationRequested?)null;
            var handlerMock = new TestUserEmailConfirmationRequestedHandler(evt => handledEvent = evt);

            var services = new ServiceCollection();
            services.AddSingleton<IEventHandler<UserEmailConfirmationRequested>>(handlerMock);
            using var serviceProvider = services.BuildServiceProvider();

            var expectedEvent = new UserEmailConfirmationRequested(Guid.NewGuid(), "confirm@example.com", "token123");
            var body = JsonSerializer.SerializeToUtf8Bytes(expectedEvent);

            // Act
            await _dispatcher.DispatchAsync("user.email.confirmation.requested", serviceProvider, body, CancellationToken.None);

            // Assert
            handledEvent.Should().NotBeNull();
            handledEvent!.Id.Should().Be(expectedEvent.Id);
            handledEvent.Email.Should().Be(expectedEvent.Email);
            handledEvent.Token.Should().Be(expectedEvent.Token);
        }

        private sealed class TestUserEmailConfirmationRequestedHandler : IEventHandler<UserEmailConfirmationRequested>
        {
            private readonly Action<UserEmailConfirmationRequested> _callback;

            public TestUserEmailConfirmationRequestedHandler(Action<UserEmailConfirmationRequested> callback)
            {
                _callback = callback;
            }

            public Task HandleAsync(UserEmailConfirmationRequested @event, CancellationToken cancellationToken = default)
            {
                _callback(@event);
                return Task.CompletedTask;
            }
        }
    }
}
