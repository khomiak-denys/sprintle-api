using DomainFramework;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Represents a flat comment on a <see cref="TaskItem"/>.
    /// Comments are append-only: authors or admins may delete but comments cannot be edited.
    /// </summary>
    public class TaskComment : EntityBase
    {
        /// <summary>Gets the task this comment belongs to.</summary>
        public Guid TaskId { get; private set; }

        /// <summary>Gets the user who authored the comment.</summary>
        public Guid AuthorId { get; private set; }

        /// <summary>Gets the textual content of the comment.</summary>
        public string Content { get; private set; } = null!;

        /// <summary>Navigation property back to the parent task.</summary>
        public TaskItem Task { get; private set; } = null!;

        private TaskComment() { }

        private TaskComment(Guid taskId, Guid authorId, string content)
        {
            TaskId = taskId;
            AuthorId = authorId;
            Content = content;
        }

        /// <summary>
        /// Factory method to create a new <see cref="TaskComment"/>.
        /// </summary>
        /// <param name="taskId">The parent task identifier.</param>
        /// <param name="authorId">The comment author's user identifier.</param>
        /// <param name="content">Non-empty comment text.</param>
        /// <returns>A new <see cref="TaskComment"/> instance.</returns>
        public static TaskComment Create(Guid taskId, Guid authorId, string content)
        {
            var comment = new TaskComment(taskId, authorId, content);
            comment.OnCreate();
            return comment;
        }
    }
}
