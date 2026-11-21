using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityBLL.Services;

public class AuditService : IAuditService
{
    /// <summary>
    /// The categories a moderator is allowed to read. Anything else (security
    /// events, account changes, system noise) stays administrator-only.
    /// </summary>
    private static readonly AuditCategory[] ModerationCategories =
    {
        AuditCategory.Content,
        AuditCategory.Moderation
    };

    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IdentityDbContext dbContext, ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuditLogEntryDto> RecordAsync(AuditRecordDto record, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(record.Action);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.TargetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.TargetId);

        var entry = new AuditLogEntry
        {
            ActorId = record.ActorId,
            ActorName = Truncate(record.ActorName, 256),
            Action = Truncate(record.Action, 128)!,
            TargetType = Truncate(record.TargetType, 64)!,
            TargetId = Truncate(record.TargetId, 128)!,
            TargetName = Truncate(record.TargetName, 256),
            Details = Truncate(record.Details, 4000),
            Category = record.Category,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit {Action} by {ActorId} on {TargetType} {TargetId}.",
            entry.Action, entry.ActorId, entry.TargetType, entry.TargetId);

        return ToDto(entry);
    }

    public Task<IReadOnlyList<AuditLogEntryDto>> GetAsync(AuditQueryDto query, CancellationToken cancellationToken = default)
        => QueryAsync(query, categories: null, cancellationToken);

    public Task<IReadOnlyList<AuditLogEntryDto>> GetModerationAsync(AuditQueryDto query, CancellationToken cancellationToken = default)
        => QueryAsync(query, categories: ModerationCategories, cancellationToken);

    private async Task<IReadOnlyList<AuditLogEntryDto>> QueryAsync(
        AuditQueryDto query,
        AuditCategory[]? categories,
        CancellationToken cancellationToken)
    {
        var entries = _dbContext.AuditLogEntries.AsNoTracking();

        if (categories is not null)
            entries = entries.Where(e => categories.Contains(e.Category));

        if (query.From is { } from)
            entries = entries.Where(e => e.CreatedAt >= from);

        if (query.To is { } to)
            entries = entries.Where(e => e.CreatedAt <= to);

        if (query.ActorId is { } actorId)
            entries = entries.Where(e => e.ActorId == actorId);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            entries = entries.Where(e => e.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetType))
        {
            var targetType = query.TargetType.Trim();
            entries = entries.Where(e => e.TargetType == targetType);
        }

        var results = await entries
            .OrderByDescending(e => e.CreatedAt)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return results.Select(ToDto).ToList();
    }

    private static AuditLogEntryDto ToDto(AuditLogEntry entry) => new()
    {
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        ActorId = entry.ActorId,
        ActorName = entry.ActorName,
        Action = entry.Action,
        TargetType = entry.TargetType,
        TargetId = entry.TargetId,
        TargetName = entry.TargetName,
        Details = entry.Details,
        Category = entry.Category.ToString()
    };

    /// <summary>
    /// The column widths are enforced by the schema, so trim to fit rather than
    /// letting a long actor-supplied string fail the whole write.
    /// </summary>
    private static string? Truncate(string? value, int maxLength)
        => value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}
