using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Domain.Tasks;

namespace Workspaces.Application.Tasks.GetComments
{
    /// <summary>
    /// Handles <see cref="GetTaskCommentsQuery"/> — retrieves paginated comments
    /// with batch-resolved author info and database-level pagination/sorting.
    /// </summary>
    internal sealed class GetTaskCommentsQueryHandler
        : IQueryHandler<GetTaskCommentsQuery, Result<PaginationResult<TaskCommentResult>>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUsersApiClient _usersApiClient;

        public GetTaskCommentsQueryHandler(
            ITaskRepository taskRepository,
            IUsersApiClient usersApiClient)
        {
            _taskRepository = taskRepository;
            _usersApiClient = usersApiClient;
        }

        /// <inheritdoc/>
        public async Task<Result<PaginationResult<TaskCommentResult>>> Handle(
            GetTaskCommentsQuery query,
            CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(query.TaskId, cancellationToken);
            if (task is null)
            {
                return Result<PaginationResult<TaskCommentResult>>.Failure(
                    new NotFoundError($"Task '{query.TaskId}' was not found."));
            }

            if (!query.IsAdmin
                && task.CreatedById != query.RequestingUserId
                && task.AssigneeId != query.RequestingUserId)
            {
                return Result<PaginationResult<TaskCommentResult>>.Failure(
                    new ForbiddenError("You are not allowed to view comments on this task."));
            }

            var commentsPage = await _taskRepository.GetCommentsByTaskIdAsync(
                query.TaskId,
                query.Page,
                query.PageSize,
                cancellationToken);

            if (commentsPage.Items.Count == 0)
            {
                var emptyResult = PaginationResult<TaskCommentResult>.Create(
                    new List<TaskCommentResult>(),
                    query.Page,
                    query.PageSize,
                    commentsPage.TotalCount);

                return Result<PaginationResult<TaskCommentResult>>.Success(emptyResult);
            }

            var distinctAuthorIds = commentsPage.Items
                .Select(c => c.AuthorId)
                .Distinct()
                .ToList();

            IReadOnlyDictionary<Guid, UserResult> authors = new Dictionary<Guid, UserResult>();
            if (distinctAuthorIds.Count > 0)
            {
                authors = await _usersApiClient.GetBatchAsync(
                    distinctAuthorIds,
                    1,
                    Math.Clamp(distinctAuthorIds.Count, 1, 100),
                    cancellationToken: cancellationToken);
            }

            var commentResults = commentsPage.Items
                .Select(c => new TaskCommentResult(
                    c.Id,
                    c.AuthorId,
                    authors.TryGetValue(c.AuthorId, out var author) ? author : null,
                    c.Content,
                    c.CreatedAt))
                .ToList();

            var result = PaginationResult<TaskCommentResult>.Create(
                commentResults,
                commentsPage.Page,
                commentsPage.PageSize,
                commentsPage.TotalCount);

            return Result<PaginationResult<TaskCommentResult>>.Success(result);
        }
    }
}
