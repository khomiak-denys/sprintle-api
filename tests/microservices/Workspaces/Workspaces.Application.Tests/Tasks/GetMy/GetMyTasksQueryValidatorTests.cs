using FluentAssertions;
using Workspaces.Application.Tasks.GetMy;
using Xunit;

namespace Workspaces.Application.Tests.Tasks.GetMy
{
    public class GetMyTasksQueryValidatorTests
    {
        private readonly GetMyTasksQueryValidator _validator = new();

        [Fact]
        public void Validate_Should_Pass_When_QueryHasValidUserAndDefaults()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid());
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_UserIdIsEmpty()
        {
            var query = new GetMyTasksQuery(Guid.Empty);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.UserId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_Should_Fail_When_PageIsLessThanOne(int page)
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Page: page);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.Page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_Should_Fail_When_PageSizeIsOutOfRange(int pageSize)
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), PageSize: pageSize);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.PageSize));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("all")]
        [InlineData("ALL")]
        [InlineData("created")]
        [InlineData("Created")]
        [InlineData("assigned")]
        [InlineData("ASSIGNED")]
        public void Validate_Should_Pass_When_TypeIsValid(string? type)
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Type: type);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_TypeIsInvalid()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Type: "unknown");
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.Type));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("all")]
        [InlineData("ALL")]
        [InlineData("Todo")]
        [InlineData("InProgress")]
        [InlineData("InReview")]
        [InlineData("Done")]
        [InlineData("Cancelled")]
        public void Validate_Should_Pass_When_StatusIsValid(string? status)
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Status: status);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_StatusIsInvalid()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Status: "InvalidStatus");
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.Status));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("all")]
        [InlineData("ALL")]
        [InlineData("Low")]
        [InlineData("Medium")]
        [InlineData("High")]
        [InlineData("Critical")]
        public void Validate_Should_Pass_When_PriorityIsValid(string? priority)
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Priority: priority);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_PriorityIsInvalid()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), Priority: "BogusPriority");
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.Priority));
        }

        [Fact]
        public void Validate_Should_Pass_When_WorkspaceIdIsValidOrOmitted()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), WorkspaceId: Guid.NewGuid());
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_WorkspaceIdIsEmpty()
        {
            var query = new GetMyTasksQuery(Guid.NewGuid(), WorkspaceId: Guid.Empty);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyTasksQuery.WorkspaceId));
        }
    }
}
