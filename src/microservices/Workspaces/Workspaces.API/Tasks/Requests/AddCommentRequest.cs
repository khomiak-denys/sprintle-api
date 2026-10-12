using Workspaces.Application.Tasks.AddComment;

namespace Workspaces.API.Tasks.Requests
{
    /// <summary>
    /// HTTP request body for adding a comment to a task.
    /// </summary>
    /// <param name="Content">The comment content.</param>
    public record AddCommentRequest(string Content)
    {
        /// <summary>
        /// Converts the request into an <see cref="AddCommentCommand"/>.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="userId">The author user identifier.</param>
        /// <param name="isAdmin">Whether the user is an administrator.</param>
        /// <returns>A new <see cref="AddCommentCommand"/>.</returns>
        public AddCommentCommand ToCommand(Guid taskId, Guid userId, bool isAdmin)
        {
            return new AddCommentCommand(taskId, userId, isAdmin, Content);
        }
    }
}
