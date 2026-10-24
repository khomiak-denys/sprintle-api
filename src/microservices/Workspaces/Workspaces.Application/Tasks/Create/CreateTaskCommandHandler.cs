using DomainFramework.Errors;
using DomainFramework.Results;
using Messaging.Abstractions;
using Messaging.Contracts;
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
        private readonly IUsersApiClient _usersApiClient;
        private readonly IEventPublisher _eventPublisher;

        public CreateTaskCommandHandler(
            IUnitOfWork uow,
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            ITagRepository tagRepository,
            IUsersApiClient usersApiClient,
            IEventPublisher eventPublisher)
        {
            _uow = uow;
            _taskRepository = taskRepository;
            _workspaceRepository = workspaceRepository;
            _tagRepository = tagRepository;
            _usersApiClient = usersApiClient;
            _eventPublisher = eventPublisher;
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

            var number = await _workspaceRepository.ReserveNextTaskNumberAsync(command.WorkspaceId, cancellationToken);
            if (number is null)
            {
                return Result<Guid>.Failure(new NotFoundError($"Workspace '{command.WorkspaceId}' was not found."));
            }

            var task = TaskItem.Create(
                command.WorkspaceId,
                number,
                command.Title,
                command.Description,
                command.Type,
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

            if (command.AssigneeId.HasValue)
            {
                var assignee = await _usersApiClient.GetByIdAsync(command.AssigneeId.Value, cancellationToken);
                if (assignee is not null)
                {
                    var workspace = await _workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
                    var creator = await _usersApiClient.GetByIdAsync(command.CreatedById, cancellationToken);

                    var workspaceName = workspace?.Name ?? "Workspace";
                    var assigneeName = !string.IsNullOrWhiteSpace(assignee.FullName) ? assignee.FullName : (!string.IsNullOrWhiteSpace(assignee.UserName) ? assignee.UserName : assignee.Email);
                    var assignerName = !string.IsNullOrWhiteSpace(creator?.FullName) ? creator.FullName : (!string.IsNullOrWhiteSpace(creator?.UserName) ? creator.UserName : "A team member");

                    await _eventPublisher.PublishAsync(
                        new TaskAssigned(
                            task.Id,
                            task.Title,
                            task.Description,
                            task.WorkspaceId,
                            workspaceName,
                            assignee.Id,
                            assignee.Email,
                            assigneeName,
                            command.CreatedById,
                            assignerName,
                            task.Priority.ToString(),
                            task.Deadline,
                            TaskNumber: task.Number),
                        cancellationToken);
                }
            }

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
