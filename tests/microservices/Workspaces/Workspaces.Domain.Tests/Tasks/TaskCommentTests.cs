using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Domain.Tasks;
using Xunit;

namespace Workspaces.Domain.Tests.Tasks
{
    public class TaskCommentTests
    {
        [Fact]
        public void Create_Should_InstantiateCommentWithAuditDates_When_ValidParameters()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var content = "This is a comment";

            // Act
            var comment = TaskComment.Create(taskId, authorId, content);

            // Assert
            comment.Id.Should().NotBeEmpty();
            comment.TaskId.Should().Be(taskId);
            comment.AuthorId.Should().Be(authorId);
            comment.Content.Should().Be(content);
            comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            comment.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void AddComment_Should_AddCommentToCollectionAndModifyTask_When_CalledOnTask()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var authorId = Guid.NewGuid();
            var content = "Domain comment";

            // Act
            var comment = task.AddComment(authorId, content);

            // Assert
            task.Comments.Should().ContainSingle(c => c.Id == comment.Id);
            comment.TaskId.Should().Be(task.Id);
            comment.AuthorId.Should().Be(authorId);
            comment.Content.Should().Be(content);
            task.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void RemoveComment_Should_RemoveComment_When_RequesterIsAuthor()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var authorId = Guid.NewGuid();
            var comment = task.AddComment(authorId, "To be removed");

            // Act
            var result = task.RemoveComment(comment.Id, authorId, isAdmin: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().BeEmpty();
        }

        [Fact]
        public void RemoveComment_Should_RemoveComment_When_RequesterIsAdmin()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var authorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var comment = task.AddComment(authorId, "To be removed by admin");

            // Act
            var result = task.RemoveComment(comment.Id, adminId, isAdmin: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            task.Comments.Should().BeEmpty();
        }

        [Fact]
        public void RemoveComment_Should_ReturnForbiddenError_When_RequesterIsNotAuthorNorAdmin()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var comment = task.AddComment(authorId, "Comment text");

            // Act
            var result = task.RemoveComment(comment.Id, otherUserId, isAdmin: false);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            task.Comments.Should().ContainSingle();
        }

        [Fact]
        public void RemoveComment_Should_ReturnNotFoundError_When_CommentDoesNotExist()
        {
            // Arrange
            var task = TaskItem.Create(Guid.NewGuid(), "DMS-1", "Task", null, Priority.Medium, null, null, Guid.NewGuid());
            var randomCommentId = Guid.NewGuid();

            // Act
            var result = task.RemoveComment(randomCommentId, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }
    }
}
