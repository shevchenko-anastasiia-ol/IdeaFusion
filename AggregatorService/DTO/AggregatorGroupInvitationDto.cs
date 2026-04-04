namespace AggregatorService.DTO;

public class AggregatorGroupInvitationDto
{
    public string InvitationId { get; set; } = null!;
    public string TeamId { get; set; } = null!;
    public string TeamName { get; set; } = null!;
    public string InvitedUserId { get; set; } = null!;
    public string InvitedByUserId { get; set; } = null!;
    public string InvitedByUsername { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? Message { get; set; }
    public string Status { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}