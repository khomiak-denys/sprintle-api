using EmailTemplates;
using FluentAssertions;
using Xunit;

namespace Notifications.Application.Tests.Templates
{
    public class TaskTimeLoggedTemplateTests
    {
        [Fact]
        public void Render_Should_GenerateHtmlWithTimeDetailsAndUser_When_ValidInputsProvided()
        {
            // Arrange
            var taskTitle = "Implement OAuth2";
            var workspaceName = "Auth Core";
            var minutesSpent = 90;
            var description = "Added token refresh logic and unit tests.";
            var loggedDate = new DateOnly(2026, 10, 14);
            var loggedByUserName = "alex_dev";
            var recipient = "lead_sarah";

            // Act
            var html = TaskTimeLoggedTemplate.Render(
                taskTitle,
                workspaceName,
                minutesSpent,
                description,
                loggedDate,
                loggedByUserName,
                recipient);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("<!DOCTYPE html>");
            html.Should().Contain("Time Logged on Task");
            html.Should().Contain("Implement OAuth2");
            html.Should().Contain("Auth Core");
            html.Should().Contain("1h 30m");
            html.Should().Contain("90 minutes");
            html.Should().Contain("Added token refresh logic and unit tests.");
            html.Should().Contain("Oct 14, 2026");
            html.Should().Contain("alex_dev");
            html.Should().Contain("lead_sarah");
        }

        [Fact]
        public void Render_Should_OmitNotesSection_When_DescriptionIsNullOrWhiteSpace()
        {
            // Arrange
            var taskTitle = "Fix Bug";
            var workspaceName = "Platform";
            var loggedDate = new DateOnly(2026, 10, 14);

            // Act
            var html = TaskTimeLoggedTemplate.Render(
                taskTitle,
                workspaceName,
                45,
                null,
                loggedDate,
                "user",
                null);

            // Assert
            html.Should().NotContain("Notes:");
            html.Should().Contain("45m");
            html.Should().Contain("Hello,");
        }

        [Fact]
        public void Render_Should_EscapeHtml_When_InputsContainHtmlCharacters()
        {
            // Arrange
            var taskTitle = "<script>alert('xss')</script>";
            var workspaceName = "<b>Workspace</b>";
            var description = "<img src=x onerror=alert(1)>";
            var loggedByUserName = "<i>Hacker</i>";
            var recipient = "<u>Target</u>";

            // Act
            var html = TaskTimeLoggedTemplate.Render(
                taskTitle,
                workspaceName,
                60,
                description,
                new DateOnly(2026, 10, 14),
                loggedByUserName,
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
            var act = () => TaskTimeLoggedTemplate.Render(invalidTitle!, "Workspace", 60, null, new DateOnly(2026, 1, 1), "user");

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
            var act = () => TaskTimeLoggedTemplate.Render("Task", invalidWorkspace!, 60, null, new DateOnly(2026, 1, 1), "user");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0, "0m")]
        [InlineData(-5, "0m")]
        [InlineData(30, "30m")]
        [InlineData(60, "1h")]
        [InlineData(75, "1h 15m")]
        [InlineData(120, "2h")]
        [InlineData(150, "2h 30m")]
        public void FormatDuration_Should_FormatMinutesCorrectly(int minutes, string expected)
        {
            // Act
            var result = TaskTimeLoggedTemplate.FormatDuration(minutes);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetSubject_Should_ReturnCustomSubjectWithWorkspace_When_WorkspaceProvided()
        {
            // Act
            var subject = TaskTimeLoggedTemplate.GetSubject("Implement Auth", 90, "Platform");

            // Assert
            subject.Should().Be("Time logged on 'Implement Auth' (1h 30m) in 'Platform' - Sprintle");
        }

        [Fact]
        public void GetSubject_Should_ReturnCustomSubjectWithoutWorkspace_When_WorkspaceNullOrEmpty()
        {
            // Act
            var subject = TaskTimeLoggedTemplate.GetSubject("Implement Auth", 60, null);

            // Assert
            subject.Should().Be("Time logged on 'Implement Auth' (1h) - Sprintle");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSubject_Should_ReturnDefaultSubject_When_TaskTitleIsNullOrWhiteSpace(string? invalidTitle)
        {
            // Act
            var subject = TaskTimeLoggedTemplate.GetSubject(invalidTitle!, 60, "Platform");

            // Assert
            subject.Should().Be(TaskTimeLoggedTemplate.DefaultSubject);
        }
    }
}
