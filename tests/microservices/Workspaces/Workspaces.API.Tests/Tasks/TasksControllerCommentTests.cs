using System.Security.Claims;
using DomainFramework;
using DomainFramework.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workspaces.API.Tasks;
using Workspaces.API.Tasks.Requests;
using Workspaces.Application.Tasks.AddComment;
using Workspaces.Application.Tasks.DeleteComment;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetComments;
using Xunit;

namespace Workspaces.API.Tests.Tasks
{
    public class TasksControllerCommentTests
    {
        private readonly FakeMediator _mediator = new();
        private readonly TasksController _controller;

        public TasksControllerCommentTests()
        {
            _controller = new TasksController(_mediator);
        }

        [Fact]
        public async Task GetComments_Should_ReturnUnauthorized_When_UserIdIsEmpty()
        {
            // Arrange
            SetUserContext(null);

            // Act
            var result = await _controller.GetComments(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetComments_Should_DispatchQueryAndReturnOk_When_Authorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            SetUserContext(userId);

            var pagedResult = PaginationResult<TaskCommentResult>.Create(Array.Empty<TaskCommentResult>(), 1, 20, 0);
            _mediator.GetCommentsResult = Result<PaginationResult<TaskCommentResult>>.Success(pagedResult);

            // Act
            var result = await _controller.GetComments(taskId, 1, 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.SentQuery.Should().NotBeNull();
            _mediator.SentQuery!.TaskId.Should().Be(taskId);
            _mediator.SentQuery.RequestingUserId.Should().Be(userId);
        }

        [Fact]
        public async Task AddComment_Should_ReturnUnauthorized_When_UserIdIsEmpty()
        {
            // Arrange
            SetUserContext(null);
            var request = new AddCommentRequest("New comment");

            // Act
            var result = await _controller.AddComment(Guid.NewGuid(), request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task AddComment_Should_DispatchCommandAndReturnOk_When_Authorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            SetUserContext(userId);

            _mediator.AddCommentResult = Result<Guid>.Success(commentId);
            var request = new AddCommentRequest("New comment");

            // Act
            var result = await _controller.AddComment(taskId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(commentId);
            _mediator.SentAddCommand.Should().NotBeNull();
            _mediator.SentAddCommand!.TaskId.Should().Be(taskId);
            _mediator.SentAddCommand.UserId.Should().Be(userId);
            _mediator.SentAddCommand.Content.Should().Be("New comment");
        }

        [Fact]
        public async Task DeleteComment_Should_ReturnUnauthorized_When_UserIdIsEmpty()
        {
            // Arrange
            SetUserContext(null);

            // Act
            var result = await _controller.DeleteComment(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task DeleteComment_Should_DispatchCommandAndReturnNoContent_When_Authorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            SetUserContext(userId);

            _mediator.DeleteCommentResult = Result.Success();

            // Act
            var result = await _controller.DeleteComment(taskId, commentId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediator.SentDeleteCommand.Should().NotBeNull();
            _mediator.SentDeleteCommand!.TaskId.Should().Be(taskId);
            _mediator.SentDeleteCommand.CommentId.Should().Be(commentId);
            _mediator.SentDeleteCommand.UserId.Should().Be(userId);
        }

        private void SetUserContext(Guid? userId, string role = "User")
        {
            var claims = new List<Claim>();
            if (userId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }

            claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private sealed class FakeMediator : IMediator
        {
            public Result<PaginationResult<TaskCommentResult>>? GetCommentsResult { get; set; }
            public Result<Guid>? AddCommentResult { get; set; }
            public Result? DeleteCommentResult { get; set; }

            public GetTaskCommentsQuery? SentQuery { get; private set; }
            public AddCommentCommand? SentAddCommand { get; private set; }
            public DeleteCommentCommand? SentDeleteCommand { get; private set; }

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                if (request is GetTaskCommentsQuery query && GetCommentsResult != null)
                {
                    SentQuery = query;
                    return Task.FromResult((TResponse)(object)GetCommentsResult);
                }

                if (request is AddCommentCommand addCommand && AddCommentResult != null)
                {
                    SentAddCommand = addCommand;
                    return Task.FromResult((TResponse)(object)AddCommentResult);
                }

                if (request is DeleteCommentCommand deleteCommand && DeleteCommentResult != null)
                {
                    SentDeleteCommand = deleteCommand;
                    return Task.FromResult((TResponse)(object)DeleteCommentResult);
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
