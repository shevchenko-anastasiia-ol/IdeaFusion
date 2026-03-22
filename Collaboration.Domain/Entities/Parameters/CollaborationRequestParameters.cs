namespace Collaboration.Domain.Entities.Parameters;

public class CollaborationRequestParameters : QueryStringParameters
{
    public string? TeamId { get; set; }
    public string? FromUserId { get; set; }
    public string? ToUserId { get; set; }
    public string? Role { get; set; }
    public CollaborationRequestStatus? Status { get; set; }
    public string? CursorId { get; set; }
}