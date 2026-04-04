namespace AggregatorService.DTO;

public class AggregatorCollaborationRequestDto
{
    public string RequestId { get; set; } = null!;
    public string TeamId { get; set; } = null!;
    public string TeamName { get; set; } = null!;
    public string FromUserId { get; set; } = null!;
    public string FromUsername { get; set; } = null!;
    public string? FromUserAvatarUrl { get; set; }
    public string? ToUserId { get; set; }
    public string Role { get; set; } = null!;
    public string? Message { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}