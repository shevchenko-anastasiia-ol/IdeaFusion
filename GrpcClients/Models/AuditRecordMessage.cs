namespace GrpcClients.Models;

/// <summary>
/// A privileged action to be appended to Identity's audit trail. Sent over the
/// <c>RecordAudit</c> gRPC so the trail lives in one place regardless of which
/// service performed the action.
/// </summary>
public class AuditRecordMessage
{
    /// <summary>Keycloak subject ("sub") of the acting user.</summary>
    public string ActorId { get; set; } = string.Empty;

    public string ActorName { get; set; } = string.Empty;

    /// <summary>What happened, e.g. "post.delete".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Kind of object acted upon, e.g. "post".</summary>
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    /// <summary>Free-form detail, usually a compact JSON object.</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>Category name, matching Identity's AuditCategory enum. Empty means "user".</summary>
    public string Category { get; set; } = string.Empty;
}
