using IdentityServiceDomain.Entities;

namespace IdentityBLL.DTOs;

/// <summary>An audit entry on its way into the trail.</summary>
public class AuditRecordDto
{
    public Guid ActorId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public string? TargetName { get; set; }
    public string? Details { get; set; }
    public AuditCategory Category { get; set; } = AuditCategory.User;
}

/// <summary>An audit entry as returned to a caller.</summary>
public class AuditLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ActorId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public string? TargetName { get; set; }
    public string? Details { get; set; }
    public string Category { get; set; } = null!;
}

/// <summary>Filters for an audit read. Every field is optional and they combine with AND.</summary>
public class AuditQueryDto
{
    public const int MaxLimit = 500;

    private int _limit = 100;
    public int Limit
    {
        get => _limit;
        set => _limit = value is < 1 ? 1 : value > MaxLimit ? MaxLimit : value;
    }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? ActorId { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
}
