using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Extensions;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class AuthorizationExtensionsTests
{
    private static ServiceProvider Build() =>
        new ServiceCollection().AddIdeaFusionAuthorization().BuildServiceProvider();

    [Fact]
    public void AddIdeaFusionAuthorization_RegistersCurrentUserAccessorAsScoped()
    {
        var services = new ServiceCollection();

        services.AddIdeaFusionAuthorization();

        var descriptor = services.Single(d => d.ServiceType == typeof(ICurrentUserAccessor));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(ServiceDefaults.Auth.CurrentUserAccessor));
    }

    [Fact]
    public void AddIdeaFusionAuthorization_RegistersHttpContextAccessor()
    {
        var sp = Build();

        sp.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddIdeaFusionAuthorization_RegistersTheThreeNamedPolicies()
    {
        var provider = Build().GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var name in new[]
                 {
                     IdeaFusionPolicies.RequireAuthor,
                     IdeaFusionPolicies.RequireModerator,
                     IdeaFusionPolicies.RequireAdministrator
                 })
        {
            var policy = await provider.GetPolicyAsync(name);

            policy.Should().NotBeNull($"policy '{name}' must be registered");
            policy!.Requirements.Should().ContainSingle(r => r is RolesAuthorizationRequirement);
        }
    }

    [Fact]
    public async Task AddIdeaFusionAuthorization_PolicyRoleSetsFollowTheCompositeHierarchy()
    {
        var provider = Build().GetRequiredService<IAuthorizationPolicyProvider>();

        var author = Roles(await provider.GetPolicyAsync(IdeaFusionPolicies.RequireAuthor));
        var moderator = Roles(await provider.GetPolicyAsync(IdeaFusionPolicies.RequireModerator));
        var administrator = Roles(await provider.GetPolicyAsync(IdeaFusionPolicies.RequireAdministrator));

        author.Should().BeEquivalentTo("author", "moderator", "administrator");
        moderator.Should().BeEquivalentTo("moderator", "administrator");
        administrator.Should().BeEquivalentTo("administrator");
    }

    [Fact]
    public void AddIdeaFusionAuthorization_SetsFallbackPolicySoUnannotatedEndpointsFailClosed()
    {
        var options = Build().GetRequiredService<
            Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        options.FallbackPolicy.Should().NotBeNull();
        options.FallbackPolicy!.Requirements
            .Should().ContainSingle(r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task AddIdeaFusionAuthorization_DoesNotRegisterGameNestPolicies()
    {
        var provider = Build().GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var legacy in new[] { "RequireAdminRole", "RequireManagerRole", "RequireUserRole" })
        {
            (await provider.GetPolicyAsync(legacy)).Should().BeNull();
        }
    }

    private static string[] Roles(AuthorizationPolicy? policy) =>
        policy!.Requirements.OfType<RolesAuthorizationRequirement>()
            .SelectMany(r => r.AllowedRoles)
            .ToArray();
}
