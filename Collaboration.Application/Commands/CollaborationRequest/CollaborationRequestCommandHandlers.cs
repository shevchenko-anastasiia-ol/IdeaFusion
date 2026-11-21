using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.CollaborationRequest;

public class CreateCollaborationRequestCommandHandler
    : ICommandHandler<CreateCollaborationRequestCommand, Domain.Entities.CollaborationRequest>
{
    private readonly ICollaborationRequestRepository _requestRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public CreateCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _requestRepository = requestRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.CollaborationRequest> Handle(
        CreateCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        // ToUserId addresses the request at somebody. It is a subject of the request,
        // not the actor, so it stays: what it may not do is become the sender.
        if (!string.IsNullOrWhiteSpace(request.ToUserId)
            && string.Equals(request.ToUserId, _caller.UserId, StringComparison.Ordinal))
        {
            throw new DomainException("A collaboration request cannot be addressed to its own sender.");
        }
 
        var alreadyMember = await _teamRepository.IsUserMemberAsync(request.TeamId, _caller.UserId, cancellationToken);
        if (alreadyMember)
            throw new DomainException("User is already a member of this team.");
 
        var hasPending = await _requestRepository.HasPendingRequestAsync(
            request.TeamId, _caller.UserId, request.Role, cancellationToken);
        if (hasPending)
            throw new DomainException("A pending request for this role already exists.");
 
        var collaborationRequest = new Domain.Entities.CollaborationRequest(
            request.TeamId, _caller.UserId, request.Role, request.Message,
            request.ToUserId, _caller.Username, _caller.AvatarUrl);
 
        await _requestRepository.CreateAsync(collaborationRequest, cancellationToken);
        return collaborationRequest;
    }
}
 
public class AcceptCollaborationRequestCommandHandler
    : ICommandHandler<AcceptCollaborationRequestCommand, Domain.Entities.CollaborationRequest>
{
    private readonly ICollaborationRequestRepository _requestRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserSnapshotService _userSnapshotService;
    private readonly ICallerContext _caller;

    public AcceptCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ITeamRepository teamRepository,
        IUserSnapshotService userSnapshotService,
        ICallerContext caller)
    {
        _requestRepository = requestRepository;
        _teamRepository = teamRepository;
        _userSnapshotService = userSnapshotService;
        _caller = caller;
    }

    public async Task<Domain.Entities.CollaborationRequest> Handle(
        AcceptCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");

        var team = await _teamRepository.GetByIdAsync(collaborationRequest.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{collaborationRequest.TeamId}' not found.");

        // The decider is whoever holds the token. A sender who also holds the privilege
        // would otherwise be able to accept their own request.
        if (string.Equals(collaborationRequest.FromUserId, _caller.UserId, StringComparison.Ordinal))
            throw new ForbiddenException("A request cannot be accepted by the person who sent it.");

        string username;
        string? avatarUrl;

        if (!string.IsNullOrEmpty(collaborationRequest.FromUsername))
        {
            username = collaborationRequest.FromUsername;
            avatarUrl = collaborationRequest.FromAvatarUrl;
        }
        else
        {
            (username, avatarUrl) = await _userSnapshotService.GetUserSnapshotDataAsync(
                collaborationRequest.FromUserId, cancellationToken);
        }

        var userSnapshot = new UserSnapshot(collaborationRequest.FromUserId, username, avatarUrl);

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.RemoveMembers);

        if (!team.Members.Any(m => m.UserId == collaborationRequest.FromUserId))
            team.Join(userSnapshot, collaborationRequest.Role);

        collaborationRequest.Accept(_caller.UserId);

        await Task.WhenAll(
            _teamRepository.UpdateAsync(team, cancellationToken),
            _requestRepository.UpdateAsync(collaborationRequest, cancellationToken));

        return collaborationRequest;
    }
}
 
public class RejectCollaborationRequestCommandHandler
    : ICommandHandler<RejectCollaborationRequestCommand, Domain.Entities.CollaborationRequest>
{
    private readonly ICollaborationRequestRepository _requestRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public RejectCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _requestRepository = requestRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.CollaborationRequest> Handle(
        RejectCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");

        var team = await _teamRepository.GetByIdAsync(collaborationRequest.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{collaborationRequest.TeamId}' not found.");

        if (string.Equals(collaborationRequest.FromUserId, _caller.UserId, StringComparison.Ordinal))
            throw new ForbiddenException("A request cannot be rejected by the person who sent it.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.RemoveMembers);

        collaborationRequest.Reject(_caller.UserId);
        await _requestRepository.UpdateAsync(collaborationRequest, cancellationToken);
        return collaborationRequest;
    }
}
 
public class CancelCollaborationRequestCommandHandler : ICommandHandler<CancelCollaborationRequestCommand>
{
    private readonly ICollaborationRequestRepository _requestRepository;
    private readonly ICallerContext _caller;
 
    public CancelCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ICallerContext caller)
    {
        _requestRepository = requestRepository;
        _caller = caller;
    }
 
    public async Task<Unit> Handle(CancelCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");
 
        if (collaborationRequest.FromUserId != _caller.UserId)
            throw new ForbiddenException("Only the request author can cancel it.");
 
        collaborationRequest.Cancel(_caller.UserId);
        await _requestRepository.UpdateAsync(collaborationRequest, cancellationToken);
        return Unit.Value;
    }
}
