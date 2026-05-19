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
 
    public CreateCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ITeamRepository teamRepository)
    {
        _requestRepository = requestRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.CollaborationRequest> Handle(
        CreateCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var alreadyMember = await _teamRepository.IsUserMemberAsync(request.TeamId, request.FromUserId, cancellationToken);
        if (alreadyMember)
            throw new DomainException("User is already a member of this team.");
 
        var hasPending = await _requestRepository.HasPendingRequestAsync(
            request.TeamId, request.FromUserId, request.Role, cancellationToken);
        if (hasPending)
            throw new DomainException("A pending request for this role already exists.");
 
        var collaborationRequest = new Domain.Entities.CollaborationRequest(
            request.TeamId, request.FromUserId, request.Role, request.Message,
            request.ToUserId, request.FromUsername, request.FromAvatarUrl);
 
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

    public AcceptCollaborationRequestCommandHandler(
        ICollaborationRequestRepository requestRepository,
        ITeamRepository teamRepository,
        IUserSnapshotService userSnapshotService)
    {
        _requestRepository = requestRepository;
        _teamRepository = teamRepository;
        _userSnapshotService = userSnapshotService;
    }

    public async Task<Domain.Entities.CollaborationRequest> Handle(
        AcceptCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");

        var team = await _teamRepository.GetByIdAsync(collaborationRequest.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{collaborationRequest.TeamId}' not found.");

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

        if (!team.Members.Any(m => m.UserId == collaborationRequest.FromUserId))
            team.AddMember(userSnapshot, collaborationRequest.Role);

        collaborationRequest.Accept(request.UserId);

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
 
    public RejectCollaborationRequestCommandHandler(ICollaborationRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }
 
    public async Task<Domain.Entities.CollaborationRequest> Handle(
        RejectCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");
 
        collaborationRequest.Reject(request.UserId);
        await _requestRepository.UpdateAsync(collaborationRequest, cancellationToken);
        return collaborationRequest;
    }
}
 
public class CancelCollaborationRequestCommandHandler : ICommandHandler<CancelCollaborationRequestCommand>
{
    private readonly ICollaborationRequestRepository _requestRepository;
 
    public CancelCollaborationRequestCommandHandler(ICollaborationRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }
 
    public async Task<Unit> Handle(CancelCollaborationRequestCommand request, CancellationToken cancellationToken)
    {
        var collaborationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException($"Collaboration request '{request.RequestId}' not found.");
 
        if (collaborationRequest.FromUserId != request.UserId)
            throw new DomainException("Only the request author can cancel it.");
 
        collaborationRequest.Cancel(request.UserId);
        await _requestRepository.UpdateAsync(collaborationRequest, cancellationToken);
        return Unit.Value;
    }
}