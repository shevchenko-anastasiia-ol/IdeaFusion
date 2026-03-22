using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;

namespace Collaboration.Application.Queries.TeamPost;

public class GetTeamPostByPostIdQuery : IQuery<Domain.Entities.TeamPost?>
{
    public string PostId { get; init; } = default!;
}
 
public class GetTeamPostsByTeamQuery : IQuery<IEnumerable<Domain.Entities.TeamPost>>
{
    public string TeamId { get; init; } = default!;
}
 
public class GetTeamPostsByTeamPagedQuery : IQuery<PagedList<Domain.Entities.TeamPost>>
{
    public string TeamId { get; init; } = default!;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
 
public class GetTeamPostsByAuthorQuery : IQuery<IEnumerable<Domain.Entities.TeamPost>>
{
    public string UserId { get; init; } = default!;
}