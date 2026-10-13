using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class TaskStatusChangedTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithOldAndNewStatusAndChangedBy_When_ValidInputsProvided()
        {
            // Arrange
            var taskTitle = "Implement Payment Gateway";
            var workspaceName = "Fintech Core";
            var oldStatus = "InReview";
            var newStatus = "Done";
            var changedBy = "alice_manager";
            var taskUrl = "http://localhost:4200/dashboard?workspaceId=ws-1&taskId=task-2";
            var recipient = "bob_developer";

            // Act
            var html = TaskStatusChangedTemplate.Render(
                taskTitle,
                workspaceName,
                oldStatus,
                newStatus,
                changedBy,
                taskUrl,
                recipient);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("Task Approved");
            html.Should().Contain("Implement Payment Gateway");
            html.Should().Contain("Fintech Core");
            html.Should().Contain("InReview");
            html.Should().Contain("Done");
            html.Should().Contain("alice_manager");
            html.Should().Contain("bob_developer");
            html.Should().Contain("http://localhost:4200/dashboard?workspaceId=ws-1&amp;taskId=task-2");
            html.Should().Contain("View Task");
        }

        [Fact]
        public void Render_Should_UseDefaultValues_When_OptionalInputsOmitted()
        {
            // Arrange
            var taskTitle = "Fix Bug";
            var workspaceName = "Fintech Core";
            var taskUrl = "http://localhost:4200/dashboard";

            // Act
            var html = TaskStatusChangedTemplate.Render(
                taskTitle,
                workspaceName,
                "",
                "",
                "",
                taskUrl,
                null);

            // Assert
            html.Should().Contain("Hello,");
            html.Should().Contain("A team member");
            html.Should().Contain("Unknown");
            html.Should().Contain("Updated");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputsContainHtmlCharacters()
        {
            // Arrange
            var taskTitle = "<script>alert('xss')</script>";
            var workspaceName = "<b>Workspace</b>";
            var oldStatus = "<i>Old</i>";
            var newStatus = "<u>New</u>";
            var changedBy = "<img src=x onerror=alert(1)>";
            var taskUrl = "http://localhost:4200/dashboard?a=1&b=2";
            var recipient = "<p>Recipient</p>";

            // Act
            var html = TaskStatusChangedTemplate.Render(
                taskTitle,
                workspaceName,
                oldStatus,
                newStatus,
                changedBy,
                taskUrl,
                recipient);

            // Assert
            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("&lt;b&gt;Workspace&lt;/b&gt;");
            html.Should().Contain("&lt;i&gt;Old&lt;/i&gt;");
            html.Should().Contain("&lt;u&gt;New&lt;/u&gt;");
            html.Should().NotContain("<img src=x");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var act = () => TaskStatusChangedTemplate.Render(invalidTitle!, "Workspace", "Todo", "InProgress", "user", "http://localhost:4200");

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
            var act = () => TaskStatusChangedTemplate.Render("Task", invalidWorkspace!, "Todo", "InProgress", "user", "http://localhost:4200");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_TaskUrlIsNullOrWhiteSpace(string? invalidUrl)
        {
            // Act
            var act = () => TaskStatusChangedTemplate.Render("Task", "Workspace", "Todo", "InProgress", "user", invalidUrl!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetSubject_Should_ReturnApprovedSubject_When_NewStatusIsDone()
        {
            // Act
            var subject = TaskStatusChangedTemplate.GetSubject("Setup DB", "Done", "Core");

            // Assert
            subject.Should().Be("Task 'Setup DB' in 'Core' has been approved - TaskTracker");
        }

        [Fact]
        public void GetSubject_Should_ReturnCompletedSubject_When_NewStatusIsInReview()
        {
            // Act
            var subject = TaskStatusChangedTemplate.GetSubject("Setup DB", "InReview", "Core");

            // Assert
            subject.Should().Be("Task 'Setup DB' in 'Core' has been completed and submitted for review - TaskTracker");
        }

        [Fact]
        public void GetSubject_Should_ReturnGenericSubject_When_NewStatusIsOther()
        {
            // Act
            var subject = TaskStatusChangedTemplate.GetSubject("Setup DB", "InProgress", "Core");

            // Assert
            subject.Should().Be("Task 'Setup DB' status changed to InProgress in 'Core' - TaskTracker");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSubject_Should_ReturnDefaultSubject_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var subject = TaskStatusChangedTemplate.GetSubject(invalidTitle!, "Done");

            // Assert
            subject.Should().Be(TaskStatusChangedTemplate.DefaultSubject);
        }
    }
}
