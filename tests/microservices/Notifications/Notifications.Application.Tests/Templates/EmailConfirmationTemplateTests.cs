using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class EmailConfirmationTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithButtonAndConfirmationUrl_When_ValidUrlProvided()
        {
            // Arrange
            var url = "http://localhost:4200/confirm-email?userId=123&token=abc";

            // Act
            var html = EmailConfirmationTemplate.Render(url);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("Confirm your email address");
            html.Should().Contain("Confirm Email");
            html.Should().Contain("http://localhost:4200/confirm-email?userId=123&amp;token=abc");
            html.Should().Contain("Sprintle");
            html.Should().Contain("background-color: #0078d4");
        }

        [Fact]
        public void Render_Should_IncludeRecipientEmail_When_RecipientProvided()
        {
            // Arrange
            var url = "http://localhost:4200/confirm-email";
            var email = "john.doe@example.com";

            // Act
            var html = EmailConfirmationTemplate.Render(url, email);

            // Assert
            html.Should().Contain("john.doe@example.com");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_UseDefaultGreeting_When_RecipientEmailIsNullOrWhiteSpace(string? email)
        {
            // Arrange
            var url = "http://localhost:4200/confirm-email";

            // Act
            var html = EmailConfirmationTemplate.Render(url, email);

            // Assert
            html.Should().Contain("Hello,");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputContainsHtmlCharacters()
        {
            // Arrange
            var url = "http://localhost:4200/confirm?a=1&b=2<script>";
            var email = "<script>alert('xss')</script>";

            // Act
            var html = EmailConfirmationTemplate.Render(url, email);

            // Assert
            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_ConfirmationUrlIsNullOrWhiteSpace(string invalidUrl)
        {
            // Act
            var act = () => EmailConfirmationTemplate.Render(invalidUrl);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Render_Should_ThrowArgumentNullException_When_ConfirmationUrlIsNull()
        {
            // Act
            var act = () => EmailConfirmationTemplate.Render(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
