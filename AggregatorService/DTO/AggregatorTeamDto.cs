namespace AggregatorService.DTO;

/// <summary>
/// Повна картка команди — повертається на сторінці команди.
/// </summary>
public class AggregatorTeamDto
{
    public string TeamId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Status { get; set; } = null!;
 
    public List<string> Tags { get; set; } = new();
    public List<AggregatorTeamMemberDto> Members { get; set; } = new();
    public List<AggregatorRequiredRoleDto> RequiredRoles { get; set; } = new();
}
 

 

 
