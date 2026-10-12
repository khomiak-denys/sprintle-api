using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using FluentAssertions;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Users.GetContactInfo;
using Xunit;

namespace Users.Application.Tests.Users.GetContactInfo
{
    public class GetUserContactInfoQueryHandlerTests
    {
        private readonly FakeUserService _userService = new();
        private readonly GetUserContactInfoQueryHandler _handler;

        public GetUserContactInfoQueryHandlerTests()
        {
            _handler = new GetUserContactInfoQueryHandler(_userService);
        }

        [Fact]
        public async Task Handle_Should_ReturnContactInfo_When_UserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedContactInfo = new UserContactInfoResult(
                userId,
                "john.doe@example.com",
                "johndoe",
                "John Doe");

            _userService.ContactInfos[userId] = expectedContactInfo;

            var query = new GetUserContactInfoQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(userId);
            result.Value.Email.Should().Be("john.doe@example.com");
            result.Value.UserName.Should().Be("johndoe");
            result.Value.FullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserDoesNotExist()
        {
            // Arrange
            var query = new GetUserContactInfoQuery(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        private sealed class FakeUserService : IUserService
        {
            public Dictionary<Guid, UserContactInfoResult> ContactInfos { get; } = new();

            public Task<Result<UserContactInfoResult>> GetContactInfoByIdAsync(Guid userId, CancellationToken ct)
            {
                if (ContactInfos.TryGetValue(userId, out var contactInfo))
                {
                    return Task.FromResult(Result<UserContactInfoResult>.Success(contactInfo));
                }

                return Task.FromResult(Result<UserContactInfoResult>.Failure(new NotFoundError("User not found")));
            }

            public Task<Result<PaginationResult<UserContactInfoResult>>> GetContactInfoBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                string? search,
                bool? isWorkspaceMember,
                int page,
                int pageSize,
                CancellationToken ct) =>
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
