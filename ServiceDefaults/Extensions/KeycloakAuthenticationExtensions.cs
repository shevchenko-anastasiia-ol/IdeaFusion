using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace ServiceDefaults.Extensions;

[ExcludeFromCodeCoverage]
public static class KeycloakAuthenticationExtensions
{
    private const string Scheme = JwtBearerDefaults.AuthenticationScheme;
    private const string DefaultRealm = "IdeaFusion";
    private const string DefaultAudience = "ideafusion_api";

    /// <summary>
    /// Adds Keycloak JWT authentication for microservices.
    /// Uses configuration from appsettings or Aspire service discovery.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var realm =
            configuration["Keycloak:Realm"]
            ?? Environment.GetEnvironmentVariable("KEYCLOAK_REALM")
            ?? DefaultRealm;

        var audience =
            configuration["Keycloak:Audience"]
            ?? Environment.GetEnvironmentVariable("KEYCLOAK_AUDIENCE")
            ?? DefaultAudience;

        var keycloakUrl =
            configuration["Keycloak:Url"]
            ?? configuration["services:keycloak:https:0"]
            ?? configuration["services:keycloak:http:0"]
            ?? Environment.GetEnvironmentVariable("KEYCLOAK_URL")
            ?? "http://localhost:8080";

        services
            .AddAuthentication(options =>
            {
                // The Keycloak handler is the only scheme in these services, so it has
                // to be the default one: [Authorize] and the fail-closed fallback policy
                // both need an authenticate/challenge scheme to fall back to.
                options.DefaultAuthenticateScheme = Scheme;
                options.DefaultChallengeScheme = Scheme;
            })
            .AddKeycloakJwtBearer(Scheme,
                realm: realm,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Audience = audience;

                    options.Authority =
                        $"{keycloakUrl.TrimEnd('/')}/realms/{realm}";

                    // Keep Keycloak's own claim names. The realm emits a flat "role"
                    // claim, and remapping would move it to ClaimTypes.Role, which only
                    // RoleClaimType below would then match.
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters.RoleClaimType = "role";
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                });

        return services;
    }
}
