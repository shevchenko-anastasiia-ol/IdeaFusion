namespace AggregatorService.DTO;

public class AggregatorPostDetailDto : AggregatorPostDto
{
    public List<AggregatorCommentDto> Comments { get; set; } = new();
    public AggregatorTeamSummaryDto? Team { get; set; }
}