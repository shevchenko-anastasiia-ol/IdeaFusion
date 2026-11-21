using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;

namespace ServiceDefaults.Auth;

/// <summary>
/// <see cref="ICurrentUserAccessor"/> over <see cref="IHttpContextAccessor"/>.
/// Registered scoped, so it resolves the principal once per request.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            // "sub" first: inbound claim mapping is disabled, so the Keycloak name
            // is what actually arrives. NameIdentifier is only a fallback.
            var sub = Principal?.FindFirst("sub")?.Value
                      ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? Principal?.FindFirst("email")?.Value;

    public string? UserName =>
        Principal?.FindFirst("preferred_username")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    // Keycloak only emits "picture" when the user profile carries an avatar, so null
    // is the normal case rather than an error. Nothing may depend on it being there.
    public string? AvatarUrl => Principal?.FindFirst("picture")?.Value;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var principal = Principal;
            if (principal is null)
            {
                return Array.Empty<string>();
            }

            // The realm's "ideafusion-roles" scope emits a flat "role" claim. With
            // MapInboundClaims disabled that arrives verbatim; ClaimTypes.Role is
            // still accepted so a mapped inbound token keeps working.
            var roles = principal
                .FindAll("role")
                .Concat(principal.FindAll(ClaimTypes.Role))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return roles;
        }
    }

    public bool IsAuthor =>
        IdeaFusionRoles.AuthorOrAbove.Any(r => IsInRole(r));

    public bool IsModerator =>
        IdeaFusionRoles.ModeratorOrAbove.Any(r => IsInRole(r));

    public bool IsAdministrator => IsInRole(IdeaFusionRoles.Administrator);

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
