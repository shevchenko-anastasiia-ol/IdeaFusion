namespace IdentityBLL.DTOs;

/// <summary>
/// The caller as this service sees them: the local profile row plus the roles
/// carried by the Keycloak token. The role list is never read from the database,
/// so it always reflects the token that was actually presented.
/// </summary>
public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Specialization { get; set; }
    public string? AvatarUrl { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }

    /// <summary>True when this call created the profile row, i.e. the user is new here.</summary>
    public bool JustProvisioned { get; set; }
}

/// <summary>Whether the caller has an authenticator app set up, and where to fix that.</summary>
public class TwoFactorStatusDto
{
    public bool TwoFactorConfigured { get; set; }

    /// <summary>Keycloak account console, where 2FA is actually configured by the user.</summary>
    public string AccountConsoleUrl { get; set; } = null!;
}

/// <summary>A user as returned by a Keycloak-backed search.</summary>
public class KeycloakUserSummaryDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public bool Enabled { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool TwoFactorConfigured { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

/// <summary>Request body for granting or revoking realm roles.</summary>
public class UpdateUserRolesDto
{
    public IReadOnlyList<string> Roles { get; set; } = [];
}

/// <summary>Result of a role change: who was changed, and what they hold now.</summary>
public class UserRolesResultDto
{
    public Guid UserId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
