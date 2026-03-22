using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.TeamPost;

public class GetTeamPostByPostIdQueryHandler
    : IQueryHandler<GetTeamPostByPostIdQuery, Domain.Entities.TeamPost?>
{
    private readonly ITeamPostRepository _repository;
 
    public GetTeamPostByPostIdQueryHandler(ITeamPostRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<Domain.Entities.TeamPost?> Handle(
        GetTeamPostByPostIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByPostIdAsync(request.PostId, cancellationToken);
 
        if (result is null || result.IsDeleted)
            throw new DomainException($"TeamPost with PostId '{request.PostId}' not found.");
 
        return result;
    }
}
 
public class GetTeamPostsByTeamQueryHandler
    : IQueryHandler<GetTeamPostsByTeamQuery, IEnumerable<Domain.Entities.TeamPost>>
{
    private readonly ITeamPostRepository _repository;
 
    public GetTeamPostsByTeamQueryHandler(ITeamPostRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.TeamPost>> Handle(
        GetTeamPostsByTeamQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
public class GetTeamPostsByTeamPagedQueryHandler
    : IQueryHandler<GetTeamPostsByTeamPagedQuery, PagedList<Domain.Entities.TeamPost>>
{
    private readonly ITeamPostRepository _repository;
 
    public GetTeamPostsByTeamPagedQueryHandler(ITeamPostRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<PagedList<Domain.Entities.TeamPost>> Handle(
        GetTeamPostsByTeamPagedQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByTeamPagedAsync(
            request.TeamId, request.PageNumber, request.PageSize, cancellationToken);
    }
}
 
public class GetTeamPostsByAuthorQueryHandler
    : IQueryHandler<GetTeamPostsByAuthorQuery, IEnumerable<Domain.Entities.TeamPost>>
{
    private readonly ITeamPostRepository _repository;
 
    public GetTeamPostsByAuthorQueryHandler(ITeamPostRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.TeamPost>> Handle(
        GetTeamPostsByAuthorQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByAuthorIdAsync(request.UserId, cancellationToken);
    }
}