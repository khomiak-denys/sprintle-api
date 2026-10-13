using FluentAssertions;
using Users.API.Auth.Requests;
using Xunit;

namespace Users.API.Tests.Requests
{
    public class ConfirmEmailRequestTests
    {
        [Fact]
        public void ToCommand_Should_ReturnConfirmEmailCommand_WithResolvedEmail()
        {
            // Arrange
            var request = new ConfirmEmailRequest("original@example.com");
            var resolvedEmail = "resolved@example.com";

            // Act
            var command = request.ToCommand(resolvedEmail);

            // Assert
            command.Email.Should().Be(resolvedEmail);
        }
    }
}
