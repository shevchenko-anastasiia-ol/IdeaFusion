namespace AggregatorService.DTO;

public class AggregatorTeamMemberDto
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;
}