using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class WorkspaceMemberAddedTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithWorkspaceAndManager_When_ValidInputsProvided()
        {
            // Arrange
            var workspaceName = "Mobile Platform";
            var managerName = "Alice Manager";
            var recipientName = "Bob Developer";

            // Act
            var html = WorkspaceMemberAddedTemplate.Render(workspaceName, managerName, recipientName);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("You've been added to a workspace");
            html.Should().Contain("Mobile Platform");
            html.Should().Contain("Alice Manager");
            html.Should().Contain("Bob Developer");
            html.Should().Contain("TaskTracker");
            html.Should().Contain("background-color: #0078d4");
        }

        [Fact]
        public void Render_Should_UseDefaultManagerAndGreeting_When_OptionalInputsOmitted()
        {
            // Arrange
            var workspaceName = "Mobile Platform";

            // Act
            var html = WorkspaceMemberAddedTemplate.Render(workspaceName, "", null);

            // Assert
            html.Should().Contain("Hello,");
            html.Should().Contain("A workspace manager");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputContainsHtmlCharacters()
        {
            // Arrange
            var workspaceName = "<script>alert('xss')</script>";
            var managerName = "<b>Manager</b>";
            var recipientName = "<i>User</i>";

            // Act
            var html = WorkspaceMemberAddedTemplate.Render(workspaceName, managerName, recipientName);

            // Assert
            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("&lt;b&gt;Manager&lt;/b&gt;");
            html.Should().Contain("&lt;i&gt;User&lt;/i&gt;");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_WorkspaceNameIsNullOrWhiteSpace(string? invalidName)
        {
            // Act
            var act = () => WorkspaceMemberAddedTemplate.Render(invalidName!, "Manager");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetSubject_Should_ReturnFormattedSubject_When_WorkspaceNameProvided()
        {
            // Act
            var subject = WorkspaceMemberAddedTemplate.GetSubject("Frontend Core");

            // Assert
            subject.Should().Be("You've been added to the 'Frontend Core' workspace - TaskTracker");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSubject_Should_ReturnDefaultSubject_When_WorkspaceNameIsNullOrWhiteSpace(string? invalidName)
        {
            // Act
            var subject = WorkspaceMemberAddedTemplate.GetSubject(invalidName!);

            // Assert
            subject.Should().Be(WorkspaceMemberAddedTemplate.DefaultSubject);
        }
    }
}
