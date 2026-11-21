using Collaboration.Application.Interfaces.Queries;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.CollaborationRequest;

public class GetCollaborationRequestByIdQueryHandler
    : IQueryHandler<GetCollaborationRequestByIdQuery, Domain.Entities.CollaborationRequest?>
{
    private readonly ICollaborationRequestRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetCollaborationRequestByIdQueryHandler(
        ICollaborationRequestRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.CollaborationRequest?> Handle(
        GetCollaborationRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.RequestId, cancellationToken);
 
        if (result is null)
            throw new NotFoundException($"Collaboration request '{request.RequestId}' not found.");
 
        await EnsureCanReadAsync(result.TeamId, result.FromUserId, result.ToUserId, cancellationToken);
 
        return result;
    }
 
    /// <summary>
    /// A request carries the applicant's profile and their free-text message, so it is
    /// readable only by the applicant themselves (or the invitee) and by team members
    /// who are allowed to act on requests.
    /// </summary>
    private async Task EnsureCanReadAsync(
        string teamId, string fromUserId, string? toUserId, CancellationToken cancellationToken)
    {
        if (_caller.UserId == fromUserId || (toUserId is not null && _caller.UserId == toUserId))
            return;
 
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{teamId}' not found.");
        team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
    }
}
 
public class GetCollaborationRequestsPagedQueryHandler
    : IQueryHandler<GetCollaborationRequestsPagedQuery, PagedList<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetCollaborationRequestsPagedQueryHandler(
        ICollaborationRequestRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<PagedList<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsPagedQuery request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
 
        // Without a team the filter spans every applicant's requests in the system, so a
        // team is required and the caller has to be able to review that team's requests.
        if (string.IsNullOrWhiteSpace(p.TeamId))
            throw new DomainException("TeamId is required to list collaboration requests.");
 
        var team = await _teamRepository.GetByIdAsync(p.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{p.TeamId}' not found.");
        team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
 
        return await _repository.GetPagedFilteredAsync(
            p.TeamId, p.FromUserId, p.ToUserId, p.Role, p.Status,
            p.PageNumber, p.PageSize,
            ascending: false,
            cancellationToken);
    }
}
 
public class GetCollaborationRequestsByTeamQueryHandler
    : IQueryHandler<GetCollaborationRequestsByTeamQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public GetCollaborationRequestsByTeamQueryHandler(
        ICollaborationRequestRepository repository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsByTeamQuery request, CancellationToken cancellationToken)
    {
        // The team id in the route names the team, it never says who is asking.
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{request.TeamId}' not found.");
        team.EnsurePermission(_caller.UserId, TeamPermission.InviteMembers);
 
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
// Internal gRPC surface only — see the query's remarks. Not reachable from a controller.
public class GetCollaborationRequestByIdInternalQueryHandler
    : IQueryHandler<GetCollaborationRequestByIdInternalQuery, Domain.Entities.CollaborationRequest?>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestByIdInternalQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<Domain.Entities.CollaborationRequest?> Handle(
        GetCollaborationRequestByIdInternalQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.RequestId, cancellationToken);
    }
}
 
// Internal gRPC surface only — see the query's remarks. Not reachable from a controller.
public class GetCollaborationRequestsByTeamInternalQueryHandler
    : IQueryHandler<GetCollaborationRequestsByTeamInternalQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestsByTeamInternalQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsByTeamInternalQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
public class GetMyCollaborationRequestsQueryHandler
    : IQueryHandler<GetMyCollaborationRequestsQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
    private readonly ICallerContext _caller;
 
    public GetMyCollaborationRequestsQueryHandler(
        ICollaborationRequestRepository repository,
        ICallerContext caller)
    {
        _repository = repository;
        _caller = caller;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetMyCollaborationRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByFromUserIdAsync(_caller.UserId, cancellationToken);
    }
}
 
// Internal gRPC surface only — see the query's remarks. Not reachable from a controller.
public class GetCollaborationRequestsByUserQueryHandler
    : IQueryHandler<GetCollaborationRequestsByUserQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestsByUserQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByFromUserIdAsync(request.UserId, cancellationToken);
    }
}