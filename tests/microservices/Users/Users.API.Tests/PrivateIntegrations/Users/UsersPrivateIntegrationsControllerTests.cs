using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Users.API.PrivateIntegrations.Users;
using Users.API.Users.Requests;
using Users.Application.DTOs;
using Users.Application.Users.GetContactInfo;
using Users.Application.Users.GetContactInfoBatch;
using Xunit;

namespace Users.API.Tests.PrivateIntegrations.Users
{
    public class UsersPrivateIntegrationsControllerTests
    {
        private readonly FakeSender _sender = new();
        private readonly UsersPrivateIntegrationsController _controller;

        public UsersPrivateIntegrationsControllerTests()
        {
            _controller = new UsersPrivateIntegrationsController(_sender);
        }

        [Fact]
        public async Task GetBatchContactInfo_Should_ReturnOkWithPaginatedResult_When_Successful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var contactInfo = new UserContactInfoResult(userId, "test@example.com", "testuser", "Test User");
            var paged = PaginationResult<UserContactInfoResult>.Create(new[] { contactInfo }, 1, 10, 1);

            _sender.BatchResult = Result<PaginationResult<UserContactInfoResult>>.Success(paged);

            var request = new GetUsersBatchRequest(new[] { userId });

            // Act
            var actionResult = await _controller.GetBatchContactInfo(request, page: 1, pageSize: 10, ct: CancellationToken.None);

            // Assert
            var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<PaginationResult<UserContactInfoResult>>().Subject;
            response.TotalCount.Should().Be(1);
            response.Items.Should().HaveCount(1);
            response.Items[0].Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetBatchContactInfo_Should_PassSearchParameterToQuery_When_SearchProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var contactInfo = new UserContactInfoResult(userId, "test@example.com", "testuser", "Test User");
            var paged = PaginationResult<UserContactInfoResult>.Create(new[] { contactInfo }, 1, 10, 1);

            _sender.BatchResult = Result<PaginationResult<UserContactInfoResult>>.Success(paged);

            var request = new GetUsersBatchRequest(new[] { userId });

            // Act
            var actionResult = await _controller.GetBatchContactInfo(
                request,
                search: "test",
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            _sender.LastBatchQuery.Should().NotBeNull();
            _sender.LastBatchQuery!.Search.Should().Be("test");
        }

        [Fact]
        public async Task GetBatchContactInfo_Should_PassIsWorkspaceMemberParameterToQuery_When_IsWorkspaceMemberProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var contactInfo = new UserContactInfoResult(userId, "test@example.com", "testuser", "Test User");
            var paged = PaginationResult<UserContactInfoResult>.Create(new[] { contactInfo }, 1, 10, 1);

            _sender.BatchResult = Result<PaginationResult<UserContactInfoResult>>.Success(paged);

            var request = new GetUsersBatchRequest(new[] { userId });

            // Act
            var actionResult = await _controller.GetBatchContactInfo(
                request,
                isWorkspaceMember: false,
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            _sender.LastBatchQuery.Should().NotBeNull();
            _sender.LastBatchQuery!.IsWorkspaceMember.Should().BeFalse();
        }

        [Fact]
        public async Task GetBatchContactInfo_Should_DefaultToEmptyUserIds_When_RequestIsNull()
        {
            // Arrange
            var contactInfo = new UserContactInfoResult(Guid.NewGuid(), "avail@example.com", "avail", "Avail User");
            var paged = PaginationResult<UserContactInfoResult>.Create(new[] { contactInfo }, 1, 10, 1);

            _sender.BatchResult = Result<PaginationResult<UserContactInfoResult>>.Success(paged);

            // Act
            var actionResult = await _controller.GetBatchContactInfo(
                request: null,
                isWorkspaceMember: false,
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            _sender.LastBatchQuery.Should().NotBeNull();
            _sender.LastBatchQuery!.UserIds.Should().BeEmpty();
            _sender.LastBatchQuery!.IsWorkspaceMember.Should().BeFalse();
        }

        [Fact]
        public async Task GetBatchContactInfo_Should_ReturnBadRequest_When_FailureOccurs()
        {
            // Arrange
            _sender.BatchResult = Result<PaginationResult<UserContactInfoResult>>.Failure(
                new InvalidArgumentError("UserIds cannot be empty."));

            var request = new GetUsersBatchRequest(Array.Empty<Guid>());

            // Act
            var actionResult = await _controller.GetBatchContactInfo(request, page: 1, pageSize: 10, ct: CancellationToken.None);

            // Assert
            var badRequestResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetContactInfo_Should_ReturnOk_When_UserFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var contactInfo = new UserContactInfoResult(userId, "single@example.com", "single", "Single User");
            _sender.SingleResult = Result<UserContactInfoResult>.Success(contactInfo);

            // Act
            var actionResult = await _controller.GetContactInfo(userId, CancellationToken.None);

            // Assert
            var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<UserContactInfoResult>().Subject;
            response.Id.Should().Be(userId);
            response.Email.Should().Be("single@example.com");
        }

        [Fact]
        public async Task GetContactInfo_Should_ReturnNotFound_When_UserNotFound()
        {
            // Arrange
            _sender.SingleResult = Result<UserContactInfoResult>.Failure(new NotFoundError("User not found"));

            // Act
            var actionResult = await _controller.GetContactInfo(Guid.NewGuid(), CancellationToken.None);

            // Assert
            var notFoundResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        private sealed class FakeSender : ISender
        {
            public Result<PaginationResult<UserContactInfoResult>>? BatchResult { get; set; }
            public Result<UserContactInfoResult>? SingleResult { get; set; }
            public GetUsersBatchQuery? LastBatchQuery { get; private set; }

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                if (request is GetUsersBatchQuery batchQuery && BatchResult != null)
                {
                    LastBatchQuery = batchQuery;
                    return Task.FromResult((TResponse)(object)BatchResult);
                }

                if (request is GetUserContactInfoQuery && SingleResult != null)
                {
                    return Task.FromResult((TResponse)(object)SingleResult);
                }

                throw new NotImplementedException($"No fake setup for {request.GetType().Name}");
            }

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            {
                throw new NotImplementedException();
            }

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
