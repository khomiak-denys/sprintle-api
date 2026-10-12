using System.Security.Claims;
using DomainFramework;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Authorization;
using ServiceDefaults.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;
using Workspaces.API.Workspaces.Requests;
using Workspaces.Application.Workspaces.Create;
using Workspaces.Application.Workspaces.Delete;
using Workspaces.Application.Workspaces.DTOs;
using Workspaces.Application.Workspaces.GetAll;
using Workspaces.Application.Workspaces.GetById;
using Workspaces.Application.Workspaces.Members.AddMember;
using Workspaces.Application.Workspaces.Members.GetMembers;
using Workspaces.Application.Workspaces.Members.GetPotentialMembers;
using Workspaces.Application.Workspaces.Members.RemoveMember;
using Workspaces.Application.Workspaces.Update;

namespace Workspaces.API.Workspaces
{
    /// <summary>
    /// API controller for workspace and membership management.
    /// </summary>
    [Route("api/v1/workspaces")]
    [ApiController]
    [Authorize]
    public class WorkspacesController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of <see cref="WorkspacesController"/>.
        /// </summary>
        /// <param name="mediator">The MediatR mediator instance.</param>
        public WorkspacesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gets all workspaces with pagination, optionally filtered by name.
        /// Non-admins receive workspaces they belong to; admins receive all workspaces.
        /// </summary>
        /// <param name="name">Optional name filter.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A paginated list of workspaces.</returns>
        [HttpGet]
        [SwaggerOperation(Summary = "Gets all workspaces with pagination and optional name filter")]
        [ProducesResponseType(typeof(PaginationResult<WorkspaceResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var query = new GetAllWorkspacesQuery(userId, isAdmin, name, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Gets a workspace by its unique identifier. Only the owner or an admin can access details.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Detailed workspace information including members.</returns>
        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "Gets a workspace by id with owner or admin guard")]
        [ProducesResponseType(typeof(WorkspaceDetailsResult), StatusCodes.Status200OK)]
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
            var query = new GetWorkspaceByIdQuery(id, userId, isAdmin);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Creates a new workspace. Only users with the Manager role can create workspaces.
        /// The current authenticated user becomes the owner and first member.
        /// </summary>
        /// <param name="request">Workspace creation payload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The newly created workspace identifier.</returns>
        [HttpPost]
        [Authorize(Roles = "Manager")]
        [SwaggerOperation(Summary = "Creates a new workspace (Manager only)")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            if (!User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var command = request.ToCommand(userId);
            var result = await _mediator.Send(command, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Updates an existing workspace. Only the owner or an admin can update.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="request">Workspace update payload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Updates an existing workspace")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken ct)
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

        /// <summary>
        /// Deletes a workspace. Only the owner or an admin can delete.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:guid}")]
        [SwaggerOperation(Summary = "Deletes a workspace")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new DeleteWorkspaceCommand(id, userId, isAdmin);
            var result = await _mediator.Send(command, ct);

            return result.Match(NoContent(), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Gets paginated members of a workspace with optional search across username, full name, or email.
        /// Guarded so only workspace members or admins can view.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="search">Optional search term matching username, full name, or email.</param>
        /// <param name="page">Page number (1-based, defaults to 1).</param>
        /// <param name="pageSize">Number of items per page (defaults to 3).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A paginated list of workspace members.</returns>
        [HttpGet("{id:guid}/members")]
        [SwaggerOperation(Summary = "Gets paginated members of a workspace with optional search (Admin or workspace member only)")]
        [ProducesResponseType(typeof(PaginationResult<WorkspaceMemberResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMembers(
            Guid id,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 3,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var query = new GetWorkspaceMembersQuery(id, userId, isAdmin, search, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Gets paginated potential members for a workspace with optional search across username, full name, or email.
        /// Guarded so only the workspace owner or an admin can view.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="search">Optional search term matching username, full name, or email.</param>
        /// <param name="page">Page number (1-based, defaults to 1).</param>
        /// <param name="pageSize">Number of items per page (defaults to 10).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A paginated list of potential workspace members.</returns>
        [HttpGet("{id:guid}/members/potential")]
        [SwaggerOperation(Summary = "Gets paginated potential members of a workspace with optional search (Workspace owner or admin only)")]
        [ProducesResponseType(typeof(PaginationResult<WorkspaceMemberResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPotentialMembers(
            Guid id,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var query = new GetPotentialWorkspaceMembersQuery(id, userId, isAdmin, search, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return result.Match(Ok(result.Value), error => this.ToActionResult(error));
        }

        /// <summary>
        /// Adds a member to a workspace. Only the owner or an admin can add members.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="request">The member payload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("{id:guid}/members")]
        [SwaggerOperation(Summary = "Adds a member to a workspace")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddWorkspaceMemberRequest request, CancellationToken ct)
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

        /// <summary>
        /// Removes a member from a workspace. Can be performed by owner, admin, or the member leaving.
        /// </summary>
        /// <param name="id">The workspace identifier.</param>
        /// <param name="userId">The member user identifier to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:guid}/members/{userId:guid}")]
        [SwaggerOperation(Summary = "Removes a member from a workspace")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");
            var command = new RemoveWorkspaceMemberCommand(id, userId, currentUserId, isAdmin);
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
