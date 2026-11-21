using GrpcClients.Interfaces;
using GrpcClients.Models;
using ServiceDefaults.Interfaces;

namespace ContentAPI.Services;

/// <summary>
/// Appends content-service actions to Identity's audit trail over the RecordAudit
/// gRPC, so the trail has one home rather than a log per service.
/// </summary>
public interface IContentAuditWriter
{
    Task RecordAsync(
        string action,
        string targetType,
        string targetId,
        string? targetName = null,
        string? details = null,
        string category = ContentAuditCategories.Moderation,
        CancellationToken ct = default);
}

public static class ContentAuditCategories
{
    public const string Content = "Content";
    public const string Moderation = "Moderation";
}

public sealed class ContentAuditWriter : IContentAuditWriter
{
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<ContentAuditWriter> _logger;

    public ContentAuditWriter(
        IUserGrpcClient userGrpcClient,
        ICurrentUserAccessor currentUser,
        ILogger<ContentAuditWriter> logger)
    {
        _userGrpcClient = userGrpcClient;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task RecordAsync(
        string action,
        string targetType,
        string targetId,
        string? targetName = null,
        string? details = null,
        string category = ContentAuditCategories.Moderation,
        CancellationToken ct = default)
    {
        var actorId = _currentUser.UserId;
        // Guid.Empty is a static readonly field, not a constant, so it cannot appear
        // in a pattern here; compare by value instead.
        if (actorId is null || actorId == Guid.Empty)
        {
            // Identity rejects a zero actor, and an action we cannot attribute is
            // not one worth recording. Log it loudly instead of sending it.
            _logger.LogError(
                "Refusing to record audit entry {Action} on {TargetType} {TargetId}: the caller has no subject.",
                action, targetType, targetId);
            return;
        }

        var recorded = await _userGrpcClient.RecordAuditAsync(
            new AuditRecordMessage
            {
                ActorId = actorId.Value.ToString(),
                ActorName = _currentUser.UserName ?? _currentUser.Email ?? string.Empty,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName ?? string.Empty,
                Details = details ?? string.Empty,
                Category = category
            },
            ct);

        if (!recorded)
        {
            // The gRPC client already swallows and logs transport failures, since an
            // unreachable audit sink must not undo the moderation that just happened.
            _logger.LogWarning(
                "Audit entry {Action} on {TargetType} {TargetId} was not recorded.",
                action, targetType, targetId);
        }
    }
}
