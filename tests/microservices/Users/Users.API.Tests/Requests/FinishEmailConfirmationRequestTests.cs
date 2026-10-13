using FluentAssertions;
using Users.API.Auth.Requests;
using Xunit;

namespace Users.API.Tests.Requests
{
    public class FinishEmailConfirmationRequestTests
    {
        [Fact]
        public void ToCommand_Should_ReturnFinishEmailConfirmationCommand_WithRequestValues_When_NoOverridesProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "user@example.com";
            var token = "token-abc";
            var request = new FinishEmailConfirmationRequest(token, userId, email);

            // Act
            var command = request.ToCommand();

            // Assert
            command.Token.Should().Be(token);
            command.UserId.Should().Be(userId);
            command.Email.Should().Be(email);
        }

        [Fact]
        public void ToCommand_Should_UseResolvedValues_When_OverridesProvided()
        {
            // Arrange
            var overrideUserId = Guid.NewGuid();
            var overrideEmail = "override@example.com";
            var request = new FinishEmailConfirmationRequest("token-xyz");

            // Act
            var command = request.ToCommand(overrideUserId, overrideEmail);

            // Assert
            command.Token.Should().Be("token-xyz");
            command.UserId.Should().Be(overrideUserId);
            command.Email.Should().Be(overrideEmail);
        }
    }
}
