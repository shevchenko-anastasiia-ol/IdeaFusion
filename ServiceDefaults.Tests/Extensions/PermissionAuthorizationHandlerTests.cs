using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Extensions;
using System.Security.Claims;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class PermissionRequirementTests
{
    [Fact]
    public void Constructor_SetsPermission()
    {
        var req = new PermissionRequirement("orders:read");
        req.Permission.Should().Be("orders:read");
    }

    [Fact]
    public void Constructor_NullPermission_ThrowsArgumentNullException()
    {
        var act = () => new PermissionRequirement(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PermissionRequirement_ImplementsIAuthorizationRequirement()
    {
        typeof(PermissionRequirement)
            .IsAssignableTo(typeof(IAuthorizationRequirement))
            .Should().BeTrue();
    }
}

public class PermissionAuthorizationHandlerTests
{
    private static async Task<bool> HandleAsync(ClaimsPrincipal user, string permission)
    {
        var handler     = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(permission);
        var context     = new AuthorizationHandlerContext(
            new[] { requirement }, user, null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static ClaimsPrincipal UserWithScope(string? scope = null)
    {
        var claims = scope is not null
            ? new[] { new Claim("scope", scope) }
            : Array.Empty<Claim>();
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Fact]
    public async Task Handle_NoScopeClaim_DoesNotSucceed()
    {
        var result = await HandleAsync(UserWithScope(), "orders:read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmptyScopeClaim_DoesNotSucceed()
    {
        var result = await HandleAsync(UserWithScope(""), "orders:read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ScopeContainsPermission_Succeeds()
    {
        var result = await HandleAsync(UserWithScope("orders:read payments:create"), "orders:read");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MultipleScopes_MatchesSecondScope()
    {
        var result = await HandleAsync(UserWithScope("catalog:write orders:read"), "orders:read");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ScopeDoesNotContainPermission_DoesNotSucceed()
    {
        var result = await HandleAsync(UserWithScope("catalog:write"), "orders:read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PartialPermissionMatch_DoesNotSucceed()
    {
        // "orders" is not the same as "orders:read" — must be exact
        var result = await HandleAsync(UserWithScope("orders"), "orders:read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExactSinglePermission_Succeeds()
    {
        var result = await HandleAsync(UserWithScope("payments:create"), "payments:create");
        result.Should().BeTrue();
    }

    [Fact]
    public void PermissionAuthorizationHandler_InheritsFromAuthorizationHandler()
    {
        typeof(PermissionAuthorizationHandler)
            .IsAssignableTo(typeof(AuthorizationHandler<PermissionRequirement>))
            .Should().BeTrue();
    }
}

public class PermissionExtensionsTests
{
    [Fact]
    public void AddPermissionAuthorization_RegistersPermissionAuthorizationHandlerAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddPermissionAuthorization();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IAuthorizationHandler) &&
            sd.ImplementationType == typeof(PermissionAuthorizationHandler) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddPermissionAuthorization_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddPermissionAuthorization();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task AddPermissionAuthorization_RegistersCatalogWritePolicy()
    {
        var services = new ServiceCollection();
        services.AddPermissionAuthorization();
        var sp       = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("Permission:catalog:write");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPermissionAuthorization_RegistersOrdersReadPolicy()
    {
        var services = new ServiceCollection();
        services.AddPermissionAuthorization();
        var sp       = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("Permission:orders:read");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPermissionAuthorization_RegistersPaymentsDeletePolicy()
    {
        var services = new ServiceCollection();
        services.AddPermissionAuthorization();
        var sp       = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("Permission:payments:delete");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPermissionAuthorization_RegistersReviewsWritePolicy()
    {
        var services = new ServiceCollection();
        services.AddPermissionAuthorization();
        var sp       = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("Permission:reviews:write");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPermissionPolicy_RegistersCustomPolicy()
    {
        var services = new ServiceCollection();
        services.AddPermissionPolicy("custom:action");
        var sp       = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("Permission:custom:action");

        policy.Should().NotBeNull();
    }

    [Fact]
    public void AddPermissionPolicy_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddPermissionPolicy("test:permission");

        result.Should().BeSameAs(services);
    }
}
