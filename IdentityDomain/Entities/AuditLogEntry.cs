namespace IdentityServiceDomain.Entities;

/// <summary>
/// Which slice of the audit trail an entry belongs to. Drives the coarse read
/// authorisation: administrators see everything, moderators only see the content
/// slice (<see cref="Content"/> and <see cref="Moderation"/>).
/// </summary>
public enum AuditCategory
{
    /// <summary>Authentication and session events.</summary>
    Security = 0,

    /// <summary>Profile, avatar and role changes on user accounts.</summary>
    User = 1,

    /// <summary>Posts, comments, media and other authored content.</summary>
    Content = 2,

    /// <summary>Reports, takedowns and other moderator decisions.</summary>
    Moderation = 3,

    /// <summary>Anything the platform did on its own behalf.</summary>
    System = 4
}

/// <summary>
/// One immutable audit record. Written for every privileged action, especially
/// role changes, so that "who granted what, and when" is answerable after the fact.
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Keycloak subject ("sub") of whoever performed the action.</summary>
    public Guid ActorId { get; set; }

    /// <summary>Display name of the actor at the time of the action.</summary>
    public string? ActorName { get; set; }

    /// <summary>What happened, e.g. "user.role.grant".</summary>
    public string Action { get; set; } = null!;

    /// <summary>Kind of object acted upon, e.g. "user".</summary>
    public string TargetType { get; set; } = null!;

    /// <summary>Identifier of the object acted upon. Kept as text: not every target is a GUID.</summary>
    public string TargetId { get; set; } = null!;

    /// <summary>Human-readable name of the target, so the trail stays readable without a lookup.</summary>
    public string? TargetName { get; set; }

    /// <summary>Free-form detail, usually a compact JSON object.</summary>
    public string? Details { get; set; }

    public AuditCategory Category { get; set; } = AuditCategory.User;
}
