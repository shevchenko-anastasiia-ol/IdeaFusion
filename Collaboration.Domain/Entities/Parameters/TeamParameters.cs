namespace Collaboration.Domain.Entities.Parameters;

public class TeamParameters : QueryStringParameters
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public TeamStatus? Status { get; set; }
    public string? MemberId { get; set; }
    public string? RequiredRole { get; set; }
    public string? SearchText { get; set; }
    public string? CursorId { get; set; }
}