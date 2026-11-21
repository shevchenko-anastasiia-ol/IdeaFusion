using IdentityBLL.DTOs;

namespace IdentityBLL.Interfaces;

public interface IAuditService
{
    /// <summary>Appends an entry to the trail and returns it as persisted.</summary>
    Task<AuditLogEntryDto> RecordAsync(AuditRecordDto record, CancellationToken cancellationToken = default);

    /// <summary>Full trail. Callers are responsible for holding administrator rights.</summary>
    Task<IReadOnlyList<AuditLogEntryDto>> GetAsync(AuditQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// The content slice only, for moderators. Entries outside the content and
    /// moderation categories are filtered out regardless of what the query asks for.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntryDto>> GetModerationAsync(AuditQueryDto query, CancellationToken cancellationToken = default);
}
