using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class TaskCommentAddedTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithCommentContentAndAuthor_When_ValidInputsProvided()
        {
            // Arrange
            var taskTitle = "Fix Payment Bug";
            var workspaceName = "Fintech Core";
            var commentContent = "Investigating the webhook failure on sandbox.";
            var commentAuthor = "bob_developer";
            var recipient = "alice_manager";

            // Act
            var html = TaskCommentAddedTemplate.Render(
                taskTitle,
                workspaceName,
                commentContent,
                commentAuthor,
                recipient);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("New Comment on Task");
            html.Should().Contain("Fix Payment Bug");
            html.Should().Contain("Fintech Core");
            html.Should().Contain("Investigating the webhook failure on sandbox.");
            html.Should().Contain("bob_developer");
            html.Should().Contain("alice_manager");
        }

        [Fact]
        public void Render_Should_UseDefaultValues_When_OptionalInputsOmitted()
        {
            // Arrange
            var taskTitle = "Fix Bug";
            var workspaceName = "Fintech Core";
            var commentContent = "A new comment";

            // Act
            var html = TaskCommentAddedTemplate.Render(
                taskTitle,
                workspaceName,
                commentContent,
                "",
                null);

            // Assert
            html.Should().Contain("Hello,");
            html.Should().Contain("A team member");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputsContainHtmlCharacters()
        {
            // Arrange
            var taskTitle = "<script>alert('xss')</script>";
            var workspaceName = "<b>Workspace</b>";
            var commentContent = "<img src=x onerror=alert(1)>";
            var commentAuthor = "<i>Hacker</i>";
            var recipient = "<u>Target</u>";

            // Act
            var html = TaskCommentAddedTemplate.Render(
                taskTitle,
                workspaceName,
                commentContent,
                commentAuthor,
                recipient);

            // Assert
            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("&lt;b&gt;Workspace&lt;/b&gt;");
            html.Should().NotContain("<img src=x");
            html.Should().Contain("&lt;img src=x");
            html.Should().Contain("&lt;i&gt;Hacker&lt;/i&gt;");
            html.Should().Contain("&lt;u&gt;Target&lt;/u&gt;");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var act = () => TaskCommentAddedTemplate.Render(invalidTitle!, "Workspace", "Comment", "author");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_WorkspaceNameIsNullOrWhiteSpace(string? invalidWorkspace)
        {
            // Act
            var act = () => TaskCommentAddedTemplate.Render("Task", invalidWorkspace!, "Comment", "author");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_CommentContentIsNullOrWhiteSpace(string? invalidContent)
        {
            // Act
            var act = () => TaskCommentAddedTemplate.Render("Task", "Workspace", invalidContent!, "author");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetSubject_Should_ReturnCustomSubjectWithWorkspace_When_WorkspaceProvided()
        {
            // Act
            var subject = TaskCommentAddedTemplate.GetSubject("Fix Bug", "Platform");

            // Assert
            subject.Should().Be("New comment on 'Fix Bug' in 'Platform' - Sprintle");
        }

        [Fact]
        public void GetSubject_Should_ReturnCustomSubjectWithoutWorkspace_When_WorkspaceNullOrEmpty()
        {
            // Act
            var subject = TaskCommentAddedTemplate.GetSubject("Fix Bug", null);

            // Assert
            subject.Should().Be("New comment on 'Fix Bug' - Sprintle");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSubject_Should_ReturnDefaultSubject_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var subject = TaskCommentAddedTemplate.GetSubject(invalidTitle!, "Platform");

            // Assert
            subject.Should().Be(TaskCommentAddedTemplate.DefaultSubject);
        }
    }
}
