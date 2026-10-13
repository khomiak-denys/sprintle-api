using FluentAssertions;
using Users.Application.Auth.FinishEmailConfirmation;
using Xunit;

namespace Users.Application.Tests.Auth.FinishEmailConfirmation
{
    public class FinishEmailConfirmationCommandValidatorTests
    {
        private readonly FinishEmailConfirmationCommandValidator _validator = new();

        [Fact]
        public void Validate_Should_Pass_When_TokenAndUserIdProvided()
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand("valid-token", Guid.NewGuid(), null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Pass_When_TokenAndEmailProvided()
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand("valid-token", null, "user@example.com");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_Should_Fail_When_TokenIsInvalid(string? invalidToken)
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand(invalidToken!, Guid.NewGuid(), null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(FinishEmailConfirmationCommand.Token));
        }

        [Fact]
        public void Validate_Should_Fail_When_NeitherUserIdNorEmailProvided()
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand("valid-token", null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Either UserId or Email must be provided.");
        }

        [Fact]
        public void Validate_Should_Fail_When_UserIdIsEmptyGuidAndEmailIsNull()
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand("valid-token", Guid.Empty, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Either UserId or Email must be provided.");
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("@missinguser.com")]
        public void Validate_Should_Fail_When_EmailIsInvalidFormat(string invalidEmail)
        {
            // Arrange
            var command = new FinishEmailConfirmationCommand("valid-token", null, invalidEmail);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(FinishEmailConfirmationCommand.Email));
        }
    }
}
