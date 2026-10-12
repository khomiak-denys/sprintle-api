using Workspaces.Application.Tasks.Create;
using Workspaces.Domain.Tasks;

namespace Workspaces.API.Tasks.Requests
{
    public record CreateTaskRequest(
        Guid WorkspaceId,
        string Title,
        string? Description,
        Priority Priority,
        DateTime? Deadline,
        Guid? AssigneeId,
        IEnumerable<string>? Tags)
    {
        public CreateTaskCommand ToCommand(Guid createdById, bool isAdmin = false)
        {
            return new CreateTaskCommand(
                WorkspaceId,
                createdById,
                Title,
                Description,
                Priority,
                Deadline,
                AssigneeId,
                Tags ?? Enumerable.Empty<string>(),
                isAdmin);
        }
    }
}
