using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using ServiceDefaults.Extensions;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class RequirePermissionAttributeTests
{
    [Fact]
    public void Constructor_SetsPolicyName_WithPermissionPrefix()
    {
        var attr = new RequirePermissionAttribute("orders:read");
        attr.Policy.Should().Be("Permission:orders:read");
    }

    [Theory]
    [InlineData("catalog:write",  "Permission:catalog:write")]
    [InlineData("orders:delete",  "Permission:orders:delete")]
    [InlineData("payments:read",  "Permission:payments:read")]
    [InlineData("reviews:update", "Permission:reviews:update")]
    public void Constructor_FormatsPolicy_Correctly(string permission, string expectedPolicy)
    {
        var attr = new RequirePermissionAttribute(permission);
        attr.Policy.Should().Be(expectedPolicy);
    }

    [Fact]
    public void RequirePermissionAttribute_InheritsFromAuthorizeAttribute()
    {
        typeof(RequirePermissionAttribute)
            .IsAssignableTo(typeof(AuthorizeAttribute))
            .Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyPermission_SetsPolicyWithEmptyPermissionPart()
    {
        var attr = new RequirePermissionAttribute("");
        attr.Policy.Should().Be("Permission:");
    }
}
