namespace Collaboration.Domain.Entities.Parameters;

public class GroupInvitationParameters : QueryStringParameters
{
    public string? TeamId { get; set; }
    public string? InvitedUserId { get; set; }
    public string? InvitedByUserId { get; set; }
    public string? Role { get; set; }
    public InvitationStatus? Status { get; set; }
    public bool? IsExpired { get; set; }
    public string? CursorId { get; set; }
}