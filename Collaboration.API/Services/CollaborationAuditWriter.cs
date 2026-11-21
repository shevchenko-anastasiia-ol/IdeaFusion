using Collaboration.Application.Interfaces.Services;
using GrpcClients.Interfaces;
using GrpcClients.Models;

namespace Collaboration.API.Services;

public static class CollaborationAuditCategories
{
    public const string Content = "Content";

    /// <summary>
    /// Team membership, roles and permissions: the things where one member acting on
    /// another needs to be explainable afterwards.
    /// </summary>
    public const string Moderation = "Moderation";
}

public interface ICollaborationAuditWriter
{
    Task RecordAsync(
        string action,
        string targetType,
        string targetId,
        string? targetName = null,
        string? details = null,
        string category = CollaborationAuditCategories.Moderation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends the collaboration service's privileged actions to Identity's audit trail over
/// the RecordAudit gRPC, so there is one place to ask "who removed that member" rather
/// than one log per service.
/// </summary>
public sealed class CollaborationAuditWriter : ICollaborationAuditWriter
{
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ICallerContext _caller;
    private readonly ILogger<CollaborationAuditWriter> _logger;

    public CollaborationAuditWriter(
        IUserGrpcClient userGrpcClient,
        ICallerContext caller,
        ILogger<CollaborationAuditWriter> logger)
    {
        _userGrpcClient = userGrpcClient;
        _caller = caller;
        _logger = logger;
    }

    public async Task RecordAsync(
        string action,
        string targetType,
        string targetId,
        string? targetName = null,
        string? details = null,
        string category = CollaborationAuditCategories.Moderation,
        CancellationToken cancellationToken = default)
    {
        string actorId;
        string actorName;
        try
        {
            actorId = _caller.UserId;
            actorName = _caller.Username;
        }
        catch (Exception)
        {
            // Identity rejects a zero actor, and an action nobody can be attributed to
            // is not one worth recording. Loud in the service log instead of silent.
            _logger.LogError(
                "Refusing to record audit entry {Action} on {TargetType} {TargetId}: the caller has no subject.",
                action, targetType, targetId);
            return;
        }

        var recorded = await _userGrpcClient.RecordAuditAsync(
            new AuditRecordMessage
            {
                ActorId = actorId,
                ActorName = actorName,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName ?? string.Empty,
                Details = details ?? string.Empty,
                Category = category
            },
            cancellationToken);

        if (!recorded)
        {
            // The gRPC client already logs transport failures itself: an unreachable
            // audit sink must not undo a removal that has already been applied.
            _logger.LogWarning(
                "Audit entry {Action} on {TargetType} {TargetId} was not recorded.",
                action, targetType, targetId);
        }
    }
}
