using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.GroupInvitation;

public class GetGroupInvitationByIdQueryHandler
    : IQueryHandler<GetGroupInvitationByIdQuery, Domain.Entities.GroupInvitation?>
{
    private readonly IGroupInvitationRepository _repository;
 
    public GetGroupInvitationByIdQueryHandler(IGroupInvitationRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<Domain.Entities.GroupInvitation?> Handle(
        GetGroupInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.InvitationId, cancellationToken);
 
        if (result is null)
            throw new DomainException($"Group invitation '{request.InvitationId}' not found.");
 
        return result;
    }
}
 
public class GetGroupInvitationsPagedQueryHandler
    : IQueryHandler<GetGroupInvitationsPagedQuery, PagedList<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
 
    public GetGroupInvitationsPagedQueryHandler(IGroupInvitationRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<PagedList<Domain.Entities.GroupInvitation>> Handle(
        GetGroupInvitationsPagedQuery request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        return await _repository.GetPagedFilteredAsync(
            p.TeamId, p.InvitedUserId, p.InvitedByUserId,
            p.Role, p.Status, p.IsExpired,
            p.PageNumber, p.PageSize,
            ascending: false,
            cancellationToken);
    }
}
 
public class GetGroupInvitationsByTeamQueryHandler
    : IQueryHandler<GetGroupInvitationsByTeamQuery, IEnumerable<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
 
    public GetGroupInvitationsByTeamQueryHandler(IGroupInvitationRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.GroupInvitation>> Handle(
        GetGroupInvitationsByTeamQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
public class GetGroupInvitationsByUserQueryHandler
    : IQueryHandler<GetGroupInvitationsByUserQuery, IEnumerable<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
 
    public GetGroupInvitationsByUserQueryHandler(IGroupInvitationRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.GroupInvitation>> Handle(
        GetGroupInvitationsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByInvitedUserIdAsync(request.UserId, cancellationToken);
    }
}
 
public class GetExpiredGroupInvitationsQueryHandler
    : IQueryHandler<GetExpiredGroupInvitationsQuery, IEnumerable<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
 
    public GetExpiredGroupInvitationsQueryHandler(IGroupInvitationRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.GroupInvitation>> Handle(
        GetExpiredGroupInvitationsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetExpiredAsync(cancellationToken);
    }
}