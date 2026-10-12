using FluentAssertions;
using Workspaces.Application.Workspaces.Members.GetMembers;
using Xunit;

namespace Workspaces.Application.Tests.Workspaces.Members.GetMembers
{
    public class GetWorkspaceMembersQueryValidatorTests
    {
        private readonly GetWorkspaceMembersQueryValidator _validator = new();

        [Fact]
        public void Validate_Should_Pass_When_QueryHasValidData()
        {
            var query = new GetWorkspaceMembersQuery(Guid.NewGuid(), Guid.NewGuid(), false);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_WorkspaceIdIsEmpty()
        {
            var query = new GetWorkspaceMembersQuery(Guid.Empty, Guid.NewGuid(), false);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceMembersQuery.WorkspaceId));
        }

        [Fact]
        public void Validate_Should_Fail_When_RequestingUserIdIsEmpty()
        {
            var query = new GetWorkspaceMembersQuery(Guid.NewGuid(), Guid.Empty, false);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceMembersQuery.RequestingUserId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_Should_Fail_When_PageIsLessThanOne(int page)
        {
            var query = new GetWorkspaceMembersQuery(Guid.NewGuid(), Guid.NewGuid(), false, Page: page);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceMembersQuery.Page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_Should_Fail_When_PageSizeIsOutOfRange(int pageSize)
        {
            var query = new GetWorkspaceMembersQuery(Guid.NewGuid(), Guid.NewGuid(), false, PageSize: pageSize);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceMembersQuery.PageSize));
        }
    }
}
