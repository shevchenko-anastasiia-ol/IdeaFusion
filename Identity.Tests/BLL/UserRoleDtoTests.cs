using FluentAssertions;
using IdentityBLL.DTOs;
using Xunit;

namespace Identity.Tests.BLL;

public class UserRoleDtoTests
{
    [Fact]
    public void Default_UserIdIsEmpty()
    {
        var dto = new UserRoleDto();
        dto.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Default_RoleNameIsNull()
    {
        var dto = new UserRoleDto();
        dto.RoleName.Should().BeNull();
    }

    [Fact]
    public void UserId_CanBeSetAndRetrieved()
    {
        var id  = Guid.NewGuid();
        var dto = new UserRoleDto { UserId = id };

        dto.UserId.Should().Be(id);
    }

    [Fact]
    public void RoleName_CanBeSetAndRetrieved()
    {
        var dto = new UserRoleDto { RoleName = "Admin" };

        dto.RoleName.Should().Be("Admin");
    }

    [Fact]
    public void Properties_CanBeSetTogether()
    {
        var id  = Guid.NewGuid();
        var dto = new UserRoleDto { UserId = id, RoleName = "User" };

        dto.UserId.Should().Be(id);
        dto.RoleName.Should().Be("User");
    }

    [Fact]
    public void RoleName_CanBeUpdated()
    {
        var dto = new UserRoleDto { RoleName = "User" };
        dto.RoleName = "Admin";

        dto.RoleName.Should().Be("Admin");
    }

    [Fact]
    public void UserId_CanBeUpdated()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var dto  = new UserRoleDto { UserId = id1 };
        dto.UserId = id2;

        dto.UserId.Should().Be(id2);
    }

    [Fact]
    public void IsInCorrectNamespace()
    {
        typeof(UserRoleDto).Namespace.Should().Be("IdentityBLL.DTOs");
    }

    [Fact]
    public void IsPublicClass()
    {
        typeof(UserRoleDto).IsPublic.Should().BeTrue();
    }
}