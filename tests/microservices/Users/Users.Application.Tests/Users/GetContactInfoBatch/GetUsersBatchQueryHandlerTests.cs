using DomainFramework;
using DomainFramework.Results;
using FluentAssertions;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Users.GetContactInfoBatch;
using Xunit;

namespace Users.Application.Tests.Users.GetContactInfoBatch
{
    public class GetUsersBatchQueryHandlerTests
    {
        private readonly FakeUserService _userService = new();
        private readonly GetUsersBatchQueryHandler _handler;

        public GetUsersBatchQueryHandlerTests()
        {
            _handler = new GetUsersBatchQueryHandler(_userService);
        }

        [Fact]
        public async Task Handle_Should_ReturnPaginatedUsers_When_UsersExist()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var contact1 = new UserContactInfoResult(user1Id, "user1@example.com", "user1", "User One");
            var contact2 = new UserContactInfoResult(user2Id, "user2@example.com", "user2", "User Two");

            _userService.Users[user1Id] = contact1;
            _userService.Users[user2Id] = contact2;

            var query = new GetUsersBatchQuery(new[] { user1Id, user2Id }, Page: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.TotalCount.Should().Be(2);
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items.Should().Contain(u => u.Id == user1Id && u.Email == "user1@example.com");
            result.Value.Items.Should().Contain(u => u.Id == user2Id && u.Email == "user2@example.com");
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyPaginationResult_When_UserIdsCollectionIsEmpty()
        {
            // Arrange
            var query = new GetUsersBatchQuery(Array.Empty<Guid>(), Page: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.TotalCount.Should().Be(0);
            result.Value.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_ReturnOnlyExistingUsers_When_SomeUsersDoNotExist()
        {
            // Arrange
            var existingUserId = Guid.NewGuid();
            var nonExistingUserId = Guid.NewGuid();
            var contact = new UserContactInfoResult(existingUserId, "exist@example.com", "exist", "Existing User");

            _userService.Users[existingUserId] = contact;

            var query = new GetUsersBatchQuery(new[] { existingUserId, nonExistingUserId }, Page: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Id.Should().Be(existingUserId);
        }

        [Fact]
        public async Task Handle_Should_PaginateResults_When_MultiplePagesRequested()
        {
            // Arrange
            var ids = new List<Guid>();
            for (var i = 0; i < 5; i++)
            {
                var id = Guid.NewGuid();
                ids.Add(id);
                _userService.Users[id] = new UserContactInfoResult(id, $"user{i}@example.com", $"user{i}", $"User {i}");
            }

            var queryPage1 = new GetUsersBatchQuery(ids, Page: 1, PageSize: 2);
            var queryPage2 = new GetUsersBatchQuery(ids, Page: 2, PageSize: 2);

            // Act
            var resultPage1 = await _handler.Handle(queryPage1, CancellationToken.None);
            var resultPage2 = await _handler.Handle(queryPage2, CancellationToken.None);

            // Assert
            resultPage1.IsSuccess.Should().BeTrue();
            resultPage1.Value.TotalCount.Should().Be(5);
            resultPage1.Value.Items.Should().HaveCount(2);

            resultPage2.IsSuccess.Should().BeTrue();
            resultPage2.Value.TotalCount.Should().Be(5);
            resultPage2.Value.Items.Should().HaveCount(2);
            resultPage2.Value.Items.Should().NotIntersectWith(resultPage1.Value.Items);
        }

        [Fact]
        public async Task Handle_Should_FilterBySearch_When_SearchProvided()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var contact1 = new UserContactInfoResult(user1Id, "alice@example.com", "alice", "Alice Smith");
            var contact2 = new UserContactInfoResult(user2Id, "bob@example.com", "bob", "Bob Jones");

            _userService.Users[user1Id] = contact1;
            _userService.Users[user2Id] = contact2;

            var query = new GetUsersBatchQuery(new[] { user1Id, user2Id }, Search: "alice", Page: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle(u => u.Id == user1Id);
        }

        [Fact]
        public async Task Handle_Should_FilterNonWorkspaceMembers_When_IsWorkspaceMemberIsFalse()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var nonMember1Id = Guid.NewGuid();
            var nonMember2Id = Guid.NewGuid();

            var memberContact = new UserContactInfoResult(memberId, "member@example.com", "member", "Member User");
            var nonMember1Contact = new UserContactInfoResult(nonMember1Id, "nonmember1@example.com", "nonmember1", "Non Member 1");
            var nonMember2Contact = new UserContactInfoResult(nonMember2Id, "nonmember2@example.com", "nonmember2", "Non Member 2");

            _userService.Users[memberId] = memberContact;
            _userService.Users[nonMember1Id] = nonMember1Contact;
            _userService.Users[nonMember2Id] = nonMember2Contact;

            var query = new GetUsersBatchQuery(new[] { memberId }, IsWorkspaceMember: false, Page: 1, PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(2);
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items.Should().NotContain(u => u.Id == memberId);
            result.Value.Items.Should().Contain(u => u.Id == nonMember1Id);
            result.Value.Items.Should().Contain(u => u.Id == nonMember2Id);
            _userService.LastIsWorkspaceMember.Should().BeFalse();
        }

        private sealed class FakeUserService : IUserService
        {
            public Dictionary<Guid, UserContactInfoResult> Users { get; } = new();
            public bool? LastIsWorkspaceMember { get; private set; }

            public Task<Result<PaginationResult<UserContactInfoResult>>> GetContactInfoBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                string? search,
                bool? isWorkspaceMember,
                int page,
                int pageSize,
                CancellationToken ct)
            {
                LastIsWorkspaceMember = isWorkspaceMember;

                IEnumerable<UserContactInfoResult> matched;

                if (isWorkspaceMember == false)
                {
                    var excludedIds = userIds != null ? new HashSet<Guid>(userIds) : new HashSet<Guid>();
                    matched = Users.Values.Where(u => !excludedIds.Contains(u.Id));
                }
                else
                {
                    if (userIds == null || userIds.Count == 0)
                    {
                        return Task.FromResult(Result<PaginationResult<UserContactInfoResult>>.Success(
                            PaginationResult<UserContactInfoResult>.Create(Array.Empty<UserContactInfoResult>(), page, pageSize, 0)));
                    }

                    matched = userIds
                        .Distinct()
                        .Where(id => Users.ContainsKey(id))
                        .Select(id => Users[id]);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    matched = matched.Where(u =>
                        (!string.IsNullOrEmpty(u.UserName) && u.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.FullName) && u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)));
                }

                var matchedList = matched
                    .OrderBy(u => u.UserName)
                    .ToList();

                var paged = matchedList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = PaginationResult<UserContactInfoResult>.Create(paged, page, pageSize, matchedList.Count);
                return Task.FromResult(Result<PaginationResult<UserContactInfoResult>>.Success(result));
            }

            public Task<Result<UserContactInfoResult>> GetContactInfoByIdAsync(Guid userId, CancellationToken ct) =>
                throw new NotImplementedException();

            public Task<Result<UserProfileResult>> GetByIdAsync(Guid userId, CancellationToken ct) =>
                throw new NotImplementedException();

            public Task<Result<PaginationResult<UserResult>>> GetAllAsync(int page, int pageSize, CancellationToken ct) =>
                throw new NotImplementedException();

            public Task<Result> UpdateProfileAsync(Guid userId, string? fullName, string? userName, CancellationToken ct) =>
                throw new NotImplementedException();

            public Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct) =>
                throw new NotImplementedException();

            public Task<Result> DeleteAsync(Guid userId, CancellationToken ct) =>
                throw new NotImplementedException();
        }
    }
}
