using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Auth;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;

namespace ServiceDefaults.Extensions;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers <see cref="ICurrentUserAccessor"/> and the IdeaFusion authorization
    /// policies, and makes authorization fail closed: any endpoint that does not
    /// declare its own [Authorize] attribute still requires an authenticated user.
    /// </summary>
    public static IServiceCollection AddIdeaFusionAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        services.AddAuthorizationBuilder()
            .AddPolicy(IdeaFusionPolicies.RequireAuthor,
                policy => policy.RequireRole(IdeaFusionRoles.AuthorOrAbove))
            .AddPolicy(IdeaFusionPolicies.RequireModerator,
                policy => policy.RequireRole(IdeaFusionRoles.ModeratorOrAbove))
            .AddPolicy(IdeaFusionPolicies.RequireAdministrator,
                policy => policy.RequireRole(IdeaFusionRoles.AdministratorOnly));

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
