namespace AggregatorService.DTO;

public class AggregatorUserFeedDto
{
    public int UserId { get; set; }
    public List<AggregatorPostDto> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}