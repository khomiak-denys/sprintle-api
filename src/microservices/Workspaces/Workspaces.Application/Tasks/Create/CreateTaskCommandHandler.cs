using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Workspaces.Application.Abstractions;
using Workspaces.Domain.Tags;
using Workspaces.Domain.Tasks;
using Workspaces.Domain.Workspaces;

namespace Workspaces.Application.Tasks.Create
{
    /// <summary>
    /// Handles <see cref="CreateTaskCommand"/> — creates a new <see cref="TaskItem"/> and attaches tags.
    /// </summary>
    internal sealed class CreateTaskCommandHandler : ICommandHandler<CreateTaskCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ITagRepository _tagRepository;

        public CreateTaskCommandHandler(
            IUnitOfWork uow,
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            ITagRepository tagRepository)
        {
            _uow = uow;
            _taskRepository = taskRepository;
            _workspaceRepository = workspaceRepository;
            _tagRepository = tagRepository;
        }

        /// <inheritdoc/>
        public async Task<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
        {
            if (!command.IsAdmin)
            {
                var isCreatorMember = await _workspaceRepository.IsMemberAsync(command.WorkspaceId, command.CreatedById, cancellationToken);
                if (!isCreatorMember)
                {
                    return Result<Guid>.Failure(new ForbiddenError("You must be a member of the workspace to create a task in it."));
                }
            }

            if (command.AssigneeId.HasValue)
            {
                var isAssigneeMember = await _workspaceRepository.IsMemberAsync(command.WorkspaceId, command.AssigneeId.Value, cancellationToken);
                if (!isAssigneeMember)
                {
                    return Result<Guid>.Failure(new InvalidArgumentError("The assigned user must be a member of the workspace."));
                }
            }

            var task = TaskItem.Create(
                command.WorkspaceId,
                command.Title,
                command.Description,
                command.Priority,
                command.Deadline,
                command.AssigneeId,
                command.CreatedById);

            // Resolve or create tags and attach them.
            foreach (var tagName in command.TagNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tag = await _tagRepository.GetByNameAsync(tagName, cancellationToken)
                          ?? await CreateAndRegisterTagAsync(tagName, cancellationToken);

                task.TaskTags.Add(new TaskTag(task.Id, tag.Id));
            }

            await _taskRepository.AddAsync(task, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(task.Id);
        }

        private async Task<Tag> CreateAndRegisterTagAsync(string name, CancellationToken cancellationToken)
        {
            var tag = Tag.Create(name);
            await _tagRepository.AddAsync(tag, cancellationToken);
            return tag;
        }
    }
}
