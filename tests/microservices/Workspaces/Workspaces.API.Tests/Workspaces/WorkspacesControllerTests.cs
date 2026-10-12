using System.Reflection;
using System.Security.Claims;
using DomainFramework;
using DomainFramework.Errors;
using DomainFramework.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workspaces.API.Workspaces;
using Workspaces.API.Workspaces.Requests;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Workspaces.Create;
using Workspaces.Application.Workspaces.Members.GetPotentialMembers;
using Xunit;

namespace Workspaces.API.Tests.Workspaces
{
    public class WorkspacesControllerTests
    {
        private readonly FakeMediator _mediator = new();
        private readonly WorkspacesController _controller;

        public WorkspacesControllerTests()
        {
            _controller = new WorkspacesController(_mediator);
        }

        [Fact]
        public void Create_Should_HaveAuthorizeAttributeWithManagerRole()
        {
            // Arrange
            var method = typeof(WorkspacesController).GetMethod(nameof(WorkspacesController.Create));

            // Act
            var authorizeAttr = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorizeAttr.Should().NotBeNull();
            authorizeAttr!.Roles.Should().Be("Manager");
        }

        [Fact]
        public async Task Create_Should_ReturnUnauthorized_When_UserIdIsEmpty()
        {
            // Arrange
            SetUserContext(userId: null, role: "Manager");
            var request = new CreateWorkspaceRequest("New Workspace", "Description");

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task Create_Should_ReturnForbid_When_UserIsNotManager()
        {
            // Arrange: User has role "User" (not Manager)
            var userId = Guid.NewGuid();
            SetUserContext(userId, role: "User");
            var request = new CreateWorkspaceRequest("New Workspace", "Description");

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Create_Should_ReturnForbid_When_UserIsAdminOnly()
        {
            // Arrange: User has role "Admin" (not Manager)
            var userId = Guid.NewGuid();
            SetUserContext(userId, role: "Admin");
            var request = new CreateWorkspaceRequest("New Workspace", "Description");

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Create_Should_ReturnOkWithWorkspaceId_When_UserIsManager()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            SetUserContext(userId, role: "Manager");

            _mediator.CreateResult = Result<Guid>.Success(workspaceId);

            var request = new CreateWorkspaceRequest("Architecture Lab", "Core workspace");

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(workspaceId);
        }

        [Fact]
        public async Task Create_Should_ReturnBadRequest_When_MediatorReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserContext(userId, role: "Manager");

            _mediator.CreateResult = Result<Guid>.Failure(new InvalidArgumentError("Workspace name is invalid."));

            var request = new CreateWorkspaceRequest("", null);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task GetPotentialMembers_Should_ReturnUnauthorized_When_UserIdIsEmpty()
        {
            // Arrange
            SetUserContext(userId: null, role: null);

            // Act
            var result = await _controller.GetPotentialMembers(Guid.NewGuid(), ct: CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetPotentialMembers_Should_ReturnOkWithPaginationResult_When_Successful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            SetUserContext(userId, role: "User");

            var potentialUser = new UserResult(Guid.NewGuid(), "dev@example.com", "devuser", "Developer");
            var paged = PaginationResult<UserResult>.Create(new[] { potentialUser }, 1, 10, 1);
            _mediator.GetPotentialResult = Result<PaginationResult<UserResult>>.Success(paged);

            // Act
            var result = await _controller.GetPotentialMembers(
                workspaceId,
                search: "dev",
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<PaginationResult<UserResult>>().Subject;
            response.TotalCount.Should().Be(1);
            _mediator.LastGetPotentialQuery.Should().NotBeNull();
            _mediator.LastGetPotentialQuery!.WorkspaceId.Should().Be(workspaceId);
            _mediator.LastGetPotentialQuery!.Search.Should().Be("dev");
        }

        [Fact]
        public async Task GetPotentialMembers_Should_ReturnForbidden_When_MediatorReturnsForbidden()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            SetUserContext(userId, role: "User");

            _mediator.GetPotentialResult = Result<PaginationResult<UserResult>>.Failure(
                new ForbiddenError("Only workspace owner or admin can view potential members."));

            // Act
            var result = await _controller.GetPotentialMembers(workspaceId, ct: CancellationToken.None);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        private void SetUserContext(Guid? userId, string? role)
        {
            var claims = new List<Claim>();

            if (userId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private sealed class FakeMediator : IMediator
        {
            public Result<Guid>? CreateResult { get; set; }
            public Result<PaginationResult<UserResult>>? GetPotentialResult { get; set; }
            public GetPotentialWorkspaceMembersQuery? LastGetPotentialQuery { get; private set; }

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                if (request is CreateWorkspaceCommand && CreateResult != null)
                {
                    return Task.FromResult((TResponse)(object)CreateResult);
                }

                if (request is GetPotentialWorkspaceMembersQuery query && GetPotentialResult != null)
                {
                    LastGetPotentialQuery = query;
                    return Task.FromResult((TResponse)(object)GetPotentialResult);
                }

                throw new NotImplementedException($"No setup for request type: {request.GetType().Name}");
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

            public Task Publish(object notification, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            {
                throw new NotImplementedException();
            }
        }
    }
}
