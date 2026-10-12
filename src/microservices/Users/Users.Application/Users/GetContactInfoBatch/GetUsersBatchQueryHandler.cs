using DomainFramework;
using DomainFramework.Results;
using Messaging.Abstractions;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Users.GetContactInfoBatch
{
    /// <summary>
    /// Handles <see cref="GetUsersBatchQuery"/> by delegating to the user service.
    /// </summary>
    public class GetUsersBatchQueryHandler : IQueryHandler<GetUsersBatchQuery, Result<PaginationResult<UserContactInfoResult>>>
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetUsersBatchQueryHandler"/> class.
        /// </summary>
        /// <param name="userService">The user service instance.</param>
        public GetUsersBatchQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        /// <inheritdoc />
        public Task<Result<PaginationResult<UserContactInfoResult>>> Handle(
            GetUsersBatchQuery request,
            CancellationToken cancellationToken)
        {
            return _userService.GetContactInfoBatchAsync(
                request.UserIds,
                request.Search,
                request.IsWorkspaceMember,
                request.Page,
                request.PageSize,
                cancellationToken);
        }
    }
}
