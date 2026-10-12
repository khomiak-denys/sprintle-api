using System.Security.Claims;
using DomainFramework;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;
using Workspaces.API.Tasks.Requests;
using Workspaces.Application.Tasks.AddComment;
using Workspaces.Application.Tasks.Approve;
using Workspaces.Application.Tasks.Assign;
using Workspaces.Application.Tasks.Cancel;
using Workspaces.Application.Tasks.ChangePriority;
using Workspaces.Application.Tasks.Close;
using Workspaces.Application.Tasks.DeleteComment;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetAll;
using Workspaces.Application.Tasks.GetById;
using Workspaces.Application.Tasks.GetComments;
using Workspaces.Application.Tasks.GetMy;
using Workspaces.Application.Tasks.SubmitForReview;

namespace Workspaces.API.Tasks
{
    [Route("api/v1/tasks")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Gets all tasks with optional filters and pagination")]
        [ProducesResponseType(typeof(PaginationResult<TaskResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] GetAllTasksRequest? request = null,
            CancellationToken ct = default)
        {
            var filter = request ?? new GetAllTasksRequest();
            var userId = GetUserId();
            var query = filter.ToQuery(userId == Guid.Empty ? null : userId);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpGet("my")]
        [SwaggerOperation(Summary = "Gets the current user's tasks with optional filters and pagination")]
        [ProducesResponseType(typeof(PaginationResult<TaskResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMy(
            [FromQuery] GetMyTasksRequest? request = null,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var filter = request ?? new GetMyTasksRequest();
            var query = filter.ToQuery(userId);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "Gets a task by id")]
        [ProducesResponseType(typeof(TaskDetailsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var query = new GetTaskByIdQuery(id, userId, isAdmin);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Creates a new task")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = request.ToCommand(userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Updates an existing task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var command = request.ToCommand(id, userId);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPatch("{id:guid}/priority")]
        [SwaggerOperation(Summary = "Changes the priority of a task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePriority(Guid id, [FromBody] ChangeTaskPriorityRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = request.ToCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPut("{id:guid}/assign/{userId:guid}")]
        [SwaggerOperation(Summary = "Assigns a task to a user")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Assign(Guid id, Guid userId, CancellationToken ct)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized();
            }

            var command = new AssignTaskCommand(id, userId, currentUserId);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPatch("{id:guid}/submit-for-review")]
        [SwaggerOperation(Summary = "Submits a task for review")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var command = new SubmitForReviewCommand(id, userId);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPost("{id:guid}/time-logs")]
        [SwaggerOperation(Summary = "Logs spent time on a task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LogTime(Guid id, [FromBody] LogTaskTimeRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var command = request.ToCommand(id, userId);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPatch("{id:guid}/approve")]
        [SwaggerOperation(Summary = "Approves a task from review")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new ApproveTaskCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPatch("{id:guid}/complete")]
        [SwaggerOperation(Summary = "Completes a task from review")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new ApproveTaskCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpPatch("{id:guid}/close")]
        [SwaggerOperation(Summary = "Closes a completed task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new CloseTaskCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpDelete("{id:guid}")]
        [HttpPatch("{id:guid}/cancel")]
        [SwaggerOperation(Summary = "Cancels a task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new CancelTaskCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        [HttpGet("{id:guid}/comments")]
        [SwaggerOperation(Summary = "Gets paginated comments for a task")]
        [ProducesResponseType(typeof(PaginationResult<TaskCommentResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetComments(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var query = new GetTaskCommentsQuery(id, userId, isAdmin, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpPost("{id:guid}/comments")]
        [SwaggerOperation(Summary = "Adds a comment to a task")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddComment(
            Guid id,
            [FromBody] AddCommentRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = request.ToCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        [HttpDelete("{id:guid}/comments/{commentId:guid}")]
        [SwaggerOperation(Summary = "Deletes a comment from a task")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteComment(
            Guid id,
            Guid commentId,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new DeleteCommentCommand(id, commentId, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}
