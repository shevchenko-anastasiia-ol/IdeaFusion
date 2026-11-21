using System.Security.Claims;

namespace ServiceDefaults.Interfaces;

/// <summary>
/// Read-only view of the caller, resolved from the validated Keycloak access token.
/// Safe to inject anywhere: every member degrades gracefully when there is no
/// authenticated user (background jobs, health probes, gRPC interceptors).
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>True when the request carries a successfully validated token.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Keycloak subject ("sub") as a <see cref="Guid"/>, or null when absent.</summary>
    Guid? UserId { get; }

    string? Email { get; }

    string? UserName { get; }

    /// <summary>
    /// Avatar from the token's <c>picture</c> claim, or null. Purely cosmetic: it
    /// never participates in identity, and services must treat it as a cache hint
    /// rather than as something the caller asserted.
    /// </summary>
    string? AvatarUrl { get; }

    /// <summary>Realm roles of the caller, from the flat "role" claim.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>True for authors, moderators and administrators.</summary>
    bool IsAuthor { get; }

    /// <summary>True for moderators and administrators.</summary>
    bool IsModerator { get; }

    /// <summary>True for administrators only.</summary>
    bool IsAdministrator { get; }

    bool IsInRole(string role);
}
