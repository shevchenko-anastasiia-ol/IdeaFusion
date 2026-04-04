namespace AggregatorService.DTO;

/// <summary>
/// Коротке представлення команди — використовується всередині PostDetailDto.
/// </summary>
public class AggregatorTeamSummaryDto
{
    public string TeamId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int MembersCount { get; set; }
}