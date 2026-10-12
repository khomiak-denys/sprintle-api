namespace Workspaces.Application.Tasks.DTOs
{
    /// <summary>
    /// Represents a single comment on a task.
    /// </summary>
    /// <param name="Id">The comment identifier.</param>
    /// <param name="AuthorId">The author's user identifier.</param>
    /// <param name="Author">The resolved author details.</param>
    /// <param name="Content">The comment text.</param>
    /// <param name="CreatedAt">When the comment was created.</param>
    public record TaskCommentResult(
        Guid Id,
        Guid AuthorId,
        UserResult? Author,
        string Content,
        DateTime CreatedAt);
}
