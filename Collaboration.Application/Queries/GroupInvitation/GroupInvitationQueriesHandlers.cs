using Collaboration.Application.Interfaces.Queries;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.GroupInvitation;

public class GetGroupInvitationByIdQueryHandler
    : IQueryHandler<GetGroupInvitationByIdQuery, Domain.Entities.GroupInvitation?>
{
    private readonly IGroupInvitationRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetGroupInvitationByIdQueryHandler(
        IGroupInvitationRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.GroupInvitation?> Handle(
        GetGroupInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.InvitationId, cancellationToken);
 
        if (result is null)
            throw new NotFoundException($"Group invitation '{request.InvitationId}' not found.");
 
        // An invitation names the invitee and carries the inviter's message, so it is
        // readable only by the invitee and by team members who can manage invitations.
        if (_caller.UserId != result.InvitedUserId)
        {
            var team = await _teamRepository.GetByIdAsync(result.TeamId, cancellationToken)
                ?? throw new NotFoundException($"Team '{result.TeamId}' not found.");
            team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
        }
 
        return result;
    }
}
 
public class GetGroupInvitationsPagedQueryHandler
    : IQueryHandler<GetGroupInvitationsPagedQuery, PagedList<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetGroupInvitationsPagedQueryHandler(
        IGroupInvitationRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<PagedList<Domain.Entities.GroupInvitation>> Handle(
        GetGroupInvitationsPagedQuery request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
 
        // Without a team the filter spans every invitation in the system, so a team is
        // required and the caller has to be able to manage that team's invitations.
        if (string.IsNullOrWhiteSpace(p.TeamId))
            throw new DomainException("TeamId is required to list group invitations.");
 
        var team = await _teamRepository.GetByIdAsync(p.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{p.TeamId}' not found.");
        team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
 
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
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetGroupInvitationsByTeamQueryHandler(
        IGroupInvitationRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<IEnumerable<Domain.Entities.GroupInvitation>> Handle(
        GetGroupInvitationsByTeamQuery request, CancellationToken cancellationToken)
    {
        // The team id in the route names the team, it never says who is asking.
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{request.TeamId}' not found.");
        team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
 
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
public class GetMyGroupInvitationsQueryHandler
    : IQueryHandler<GetMyGroupInvitationsQuery, IEnumerable<Domain.Entities.GroupInvitation>>
{
    private readonly IGroupInvitationRepository _repository;
    private readonly ICallerContext _caller;
 
    public GetMyGroupInvitationsQueryHandler(
        IGroupInvitationRepository repository,
        ICallerContext caller)
    {
        _repository = repository;
        _caller = caller;
    }
 
    public async Task<IEnumerable<Domain.Entities.GroupInvitation>> Handle(
        GetMyGroupInvitationsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByInvitedUserIdAsync(_caller.UserId, cancellationToken);
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