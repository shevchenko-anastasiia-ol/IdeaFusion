using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.Team;

public class GetTeamByIdQueryHandler : IQueryHandler<GetTeamByIdQuery, Domain.Entities.Team?>
{
    private readonly ITeamRepository _teamRepository;
 
    public GetTeamByIdQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team?> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
 
        if (team is null || team.IsDeleted)
            throw new DomainException($"Team '{request.TeamId}' not found.");
 
        return team;
    }
}
 
public class GetTeamsPagedQueryHandler : IQueryHandler<GetTeamsPagedQuery, PagedList<Domain.Entities.Team>>
{
    private readonly ITeamRepository _teamRepository;
 
    public GetTeamsPagedQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<PagedList<Domain.Entities.Team>> Handle(GetTeamsPagedQuery request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        return await _teamRepository.GetPagedFilteredAsync(
            p.Name, p.Category, p.Tag, p.Status,
            p.MemberId, p.RequiredRole,
            p.PageNumber, p.PageSize,
            p.OrderBy, ascending: false,
            cancellationToken);
    }
}
 
public class GetTeamsByMemberQueryHandler : IQueryHandler<GetTeamsByMemberQuery, IEnumerable<Domain.Entities.Team>>
{
    private readonly ITeamRepository _teamRepository;
 
    public GetTeamsByMemberQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<IEnumerable<Domain.Entities.Team>> Handle(GetTeamsByMemberQuery request, CancellationToken cancellationToken)
    {
        return await _teamRepository.GetByMemberIdAsync(request.UserId, cancellationToken);
    }
}
 
public class GetTeamsByStatusQueryHandler : IQueryHandler<GetTeamsByStatusQuery, PagedList<Domain.Entities.Team>>
{
    private readonly ITeamRepository _teamRepository;
 
    public GetTeamsByStatusQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<PagedList<Domain.Entities.Team>> Handle(GetTeamsByStatusQuery request, CancellationToken cancellationToken)
    {
        return await _teamRepository.GetByStatusAsync(request.Status, request.PageNumber, request.PageSize, cancellationToken);
    }
}
 
public class GetTeamsByCategoryQueryHandler : IQueryHandler<GetTeamsByCategoryQuery, PagedList<Domain.Entities.Team>>
{
    private readonly ITeamRepository _teamRepository;
 
    public GetTeamsByCategoryQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<PagedList<Domain.Entities.Team>> Handle(GetTeamsByCategoryQuery request, CancellationToken cancellationToken)
    {
        return await _teamRepository.GetByCategoryAsync(request.Category, request.PageNumber, request.PageSize, cancellationToken);
    }
}
 
public class SearchTeamsByNameQueryHandler : IQueryHandler<SearchTeamsByNameQuery, IEnumerable<Domain.Entities.Team>>
{
    private readonly ITeamRepository _teamRepository;
 
    public SearchTeamsByNameQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<IEnumerable<Domain.Entities.Team>> Handle(SearchTeamsByNameQuery request, CancellationToken cancellationToken)
    {
        return await _teamRepository.SearchByNameAsync(request.Name, cancellationToken);
    }
}