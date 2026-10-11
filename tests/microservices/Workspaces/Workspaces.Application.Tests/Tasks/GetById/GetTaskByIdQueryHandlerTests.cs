using DomainFramework;
using DomainFramework.Errors;
using FluentAssertions;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;
using Workspaces.Application.Tasks.GetById;
using Workspaces.Domain.Tasks;
using TaskStatus = Workspaces.Domain.Tasks.TaskStatus;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetById
{
    public class GetTaskByIdQueryHandlerTests
    {
        private readonly FakeTaskRepository _taskRepository = new();
        private readonly FakeUsersApiClient _usersApiClient = new();
        private readonly GetTaskByIdQueryHandler _handler;

        public GetTaskByIdQueryHandlerTests()
        {
            _handler = new GetTaskByIdQueryHandler(_taskRepository, _usersApiClient);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TaskDoesNotExist()
        {
            // Arrange
            var query = new GetTaskByIdQuery(Guid.NewGuid(), Guid.NewGuid(), false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_UserIsNotAllowed()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();

            var task = TaskItem.Create(Guid.NewGuid(), "Title", "Description", Priority.Medium, null, assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            var query = new GetTaskByIdQuery(task.Id, unauthorizedUserId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
        }

        [Fact]
        public async Task Handle_Should_ReturnTaskDetailsWithUserResults_When_Valid()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var task = TaskItem.Create(Guid.NewGuid(), "Test Task", "Test Desc", Priority.High, DateTime.UtcNow.AddDays(3), assigneeId, creatorId);
            _taskRepository.TaskToReturn = task;

            var creatorResult = new UserResult(creatorId, "creator@test.com", "creator_user", "Creator User");
            var assigneeResult = new UserResult(assigneeId, "assignee@test.com", "assignee_user", "Assignee User");

            _usersApiClient.Users[creatorId] = creatorResult;
            _usersApiClient.Users[assigneeId] = assigneeResult;

            var query = new GetTaskByIdQuery(task.Id, creatorId, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be("Test Task");
            result.Value.CreatedBy.Should().Be(creatorResult);
            result.Value.Assignee.Should().Be(assigneeResult);
            result.Value.CreatedByUser.Should().Be(creatorResult);
            result.Value.AssignedUser.Should().Be(assigneeResult);
            result.Value.CreatedBy.Id.Should().Be(creatorId);
            result.Value.Assignee!.Id.Should().Be(assigneeId);
        }

        [Fact]
        public async Task Handle_Should_AllowAdminToAccess_When_UserIsAdmin()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var adminId = Guid.NewGuid();

            var task = TaskItem.Create(Guid.NewGuid(), "Admin Task", null, Priority.Low, null, null, creatorId);
            _taskRepository.TaskToReturn = task;

            var creatorResult = new UserResult(creatorId, "creator@test.com", "creator", "Creator");
            _usersApiClient.Users[creatorId] = creatorResult;

            var query = new GetTaskByIdQuery(task.Id, adminId, true);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.CreatedBy.Should().Be(creatorResult);
            result.Value.Assignee.Should().BeNull();
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public TaskItem? TaskToReturn { get; set; }

            public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            {
                return Task.FromResult(TaskToReturn?.Id == id ? TaskToReturn : null);
            }

            public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<PaginationResult<TaskItem>> GetAllAsync(
                Guid? workspaceId,
                string? search,
                TaskStatus? status,
                Priority? priority,
                Guid? assigneeId,
                Guid? createdById,
                string? tag,
                int page,
                int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));

            public Task<PaginationResult<TaskItem>> GetMyAsync(
                Guid userId,
                Guid? workspaceId,
                string? type,
                string? search,
                TaskStatus? status,
                Priority? priority,
                string? tag,
                int page,
                int pageSize,
                CancellationToken cancellationToken) =>
                Task.FromResult(PaginationResult<TaskItem>.Create(Array.Empty<TaskItem>(), page, pageSize, 0));
        }

        private sealed class FakeUsersApiClient : IUsersApiClient
        {
            public Dictionary<Guid, UserResult> Users { get; } = new();

            public Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            {
                Users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }

            public Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
                IReadOnlyCollection<Guid> userIds,
                int page = 1,
                int pageSize = 100,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyDictionary<Guid, UserResult> result = Users
                    .Where(kvp => userIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return Task.FromResult(result);
            }
        }
    }
}
