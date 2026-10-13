using FluentAssertions;
using Users.Application.Auth.ConfirmEmail;
using Xunit;

namespace Users.Application.Tests.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandValidatorTests
    {
        private readonly ConfirmEmailCommandValidator _validator = new();

        [Fact]
        public void Validate_Should_Pass_When_ValidEmailProvided()
        {
            // Arrange
            var command = new ConfirmEmailCommand("valid.user@example.com");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-email")]
        [InlineData("@missinguser.com")]
        public void Validate_Should_Fail_When_EmailIsInvalid(string invalidEmail)
        {
            // Arrange
            var command = new ConfirmEmailCommand(invalidEmail);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmEmailCommand.Email));
        }
    }
}
