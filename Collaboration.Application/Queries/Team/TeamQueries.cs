using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;

namespace Collaboration.Application.Queries.Team;

public class GetTeamByIdQuery : IQuery<Domain.Entities.Team?>
{
    public string TeamId { get; init; } = default!;
}
 
public class GetTeamsPagedQuery : IQuery<PagedList<Domain.Entities.Team>>
{
    public TeamParameters Parameters { get; init; } = default!;
}
 
public class GetTeamsByMemberQuery : IQuery<IEnumerable<Domain.Entities.Team>>
{
    public string UserId { get; init; } = default!;
}
 
public class GetTeamsByStatusQuery : IQuery<PagedList<Domain.Entities.Team>>
{
    public TeamStatus Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
 
public class GetTeamsByCategoryQuery : IQuery<PagedList<Domain.Entities.Team>>
{
    public string Category { get; init; } = default!;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
 
public class SearchTeamsByNameQuery : IQuery<IEnumerable<Domain.Entities.Team>>
{
    public string Name { get; init; } = default!;
}