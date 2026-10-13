using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class TaskAssignedTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithTaskDetailsAndButton_When_ValidInputsProvided()
        {
            // Arrange
            var taskTitle = "Implement OAuth Flow";
            var taskDescription = "Integrate OAuth2 with Google provider.";
            var workspaceName = "Mobile Platform";
            var assignedByName = "Alice Manager";
            var priority = "Critical";
            var deadline = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
            var taskUrl = "http://localhost:4200/dashboard?workspaceId=ws-123&taskId=task-456";
            var recipientName = "Bob Developer";

            // Act
            var html = TaskAssignedTemplate.Render(
                taskTitle,
                taskDescription,
                workspaceName,
                assignedByName,
                priority,
                deadline,
                taskUrl,
                recipientName);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("You've been assigned to a task");
            html.Should().Contain("View Task");
            html.Should().Contain("Implement OAuth Flow");
            html.Should().Contain("Integrate OAuth2 with Google provider.");
            html.Should().Contain("Mobile Platform");
            html.Should().Contain("Alice Manager");
            html.Should().Contain("Bob Developer");
            html.Should().Contain("Critical");
            html.Should().Contain("Nov 20, 2026");
            html.Should().Contain("http://localhost:4200/dashboard?workspaceId=ws-123&amp;taskId=task-456");
            html.Should().Contain("TaskTracker");
            html.Should().Contain("background-color: #0078d4");
        }

        [Fact]
        public void Render_Should_UseDefaultValues_When_OptionalInputsOmitted()
        {
            // Arrange
            var taskTitle = "Fix Bug";
            var workspaceName = "Mobile Platform";
            var taskUrl = "http://localhost:4200/dashboard";

            // Act
            var html = TaskAssignedTemplate.Render(
                taskTitle,
                null,
                workspaceName,
                "",
                null,
                null,
                taskUrl,
                null);

            // Assert
            html.Should().Contain("Hello,");
            html.Should().Contain("A team member");
            html.Should().Contain("None");
            html.Should().Contain("No deadline");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputContainsHtmlCharacters()
        {
            // Arrange
            var taskTitle = "<script>alert('xss')</script>";
            var taskDescription = "<b>Bold desc</b>";
            var workspaceName = "<i>Workspace</i>";
            var assignedByName = "<u>Assigner</u>";
            var taskUrl = "http://localhost:4200/dashboard?a=1&b=2";
            var recipientName = "<img src=x onerror=alert(1)>";

            // Act
            var html = TaskAssignedTemplate.Render(
                taskTitle,
                taskDescription,
                workspaceName,
                assignedByName,
                "High",
                null,
                taskUrl,
                recipientName);

            // Assert
            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().NotContain("<b>Bold desc</b>");
            html.Should().Contain("&lt;b&gt;Bold desc&lt;/b&gt;");
            html.Should().Contain("&lt;i&gt;Workspace&lt;/i&gt;");
            html.Should().Contain("&lt;u&gt;Assigner&lt;/u&gt;");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Render_Should_ThrowArgumentException_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var act = () => TaskAssignedTemplate.Render(invalidTitle!, null, "Workspace", "Manager", null, null, "http://localhost:4200/dashboard");

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
            var act = () => TaskAssignedTemplate.Render("Task", null, invalidWorkspace!, "Manager", null, null, "http://localhost:4200/dashboard");

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
            var act = () => TaskAssignedTemplate.Render("Task", null, "Workspace", "Manager", null, null, invalidUrl!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetSubject_Should_ReturnFormattedSubjectWithWorkspace_When_WorkspaceNameProvided()
        {
            // Act
            var subject = TaskAssignedTemplate.GetSubject("Implement OAuth", "Mobile Platform");

            // Assert
            subject.Should().Be("You've been assigned to 'Implement OAuth' in 'Mobile Platform' - TaskTracker");
        }

        [Fact]
        public void GetSubject_Should_ReturnFormattedSubjectWithoutWorkspace_When_WorkspaceNameOmitted()
        {
            // Act
            var subject = TaskAssignedTemplate.GetSubject("Implement OAuth", null);

            // Assert
            subject.Should().Be("You've been assigned to 'Implement OAuth' - TaskTracker");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSubject_Should_ReturnDefaultSubject_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var subject = TaskAssignedTemplate.GetSubject(invalidTitle!);

            // Assert
            subject.Should().Be(TaskAssignedTemplate.DefaultSubject);
        }
    }
}
