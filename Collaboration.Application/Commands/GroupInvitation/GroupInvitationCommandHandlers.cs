using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using MediatR;

namespace Collaboration.Application.Commands.GroupInvitation;

public class CreateGroupInvitationCommandHandler
    : ICommandHandler<CreateGroupInvitationCommand, Domain.Entities.GroupInvitation>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public CreateGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        CreateGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        // An invitation the inviter sends to themselves is a back door into the team:
        // accepting it would add a member without any request ever being reviewed.
        if (string.Equals(request.InvitedUserId, _caller.UserId, StringComparison.Ordinal))
            throw new DomainException("An invitation cannot be sent to the person sending it.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.InviteMembers);

        var alreadyMember = await _teamRepository.IsUserMemberAsync(
            request.TeamId, request.InvitedUserId, cancellationToken);
        if (alreadyMember)
            throw new DomainException("User is already a member of this team.");
 
        var hasPending = await _invitationRepository.HasPendingInvitationAsync(
            request.TeamId, request.InvitedUserId, cancellationToken);
        if (hasPending)
            throw new DomainException("A pending invitation for this user already exists.");
 
        var invitation = new Domain.Entities.GroupInvitation(
            request.TeamId, request.InvitedUserId, _caller.UserId,
            request.Role, request.Message, request.ExpirationDays);
 
        await _invitationRepository.CreateAsync(invitation, cancellationToken);
        return invitation;
    }
}
 
public class AcceptGroupInvitationCommandHandler
    : ICommandHandler<AcceptGroupInvitationCommand, Domain.Entities.GroupInvitation>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public AcceptGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        AcceptGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        if (invitation.InvitedUserId != _caller.UserId)
            throw new ForbiddenException("Only the invited user can accept the invitation.");
 
        var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{invitation.TeamId}' not found.");

        invitation.Accept(_caller.UserId);

        // The new member is the caller, so the profile comes from the token rather than
        // from a request body that could name somebody else.
        team.Join(_caller.ToSnapshot(), invitation.Role);

        await Task.WhenAll(
            _invitationRepository.UpdateAsync(invitation, cancellationToken),
            _teamRepository.UpdateAsync(team, cancellationToken));

        return invitation;
    }
}
 
public class DeclineGroupInvitationCommandHandler
    : ICommandHandler<DeclineGroupInvitationCommand, Domain.Entities.GroupInvitation>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ICallerContext _caller;
 
    public DeclineGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ICallerContext caller)
    {
        _invitationRepository = invitationRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        DeclineGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        if (invitation.InvitedUserId != _caller.UserId)
            throw new ForbiddenException("Only the invited user can decline the invitation.");
 
        invitation.Decline(_caller.UserId);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        return invitation;
    }
}
 
public class RevokeGroupInvitationCommandHandler : ICommandHandler<RevokeGroupInvitationCommand>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public RevokeGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Unit> Handle(RevokeGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{invitation.TeamId}' not found.");

        // Membership alone is not enough to withdraw an invitation: the team privilege set
        // is what governs who manages the member pipeline.
        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.InviteMembers);

        invitation.Revoke(_caller.UserId);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Unit.Value;
    }
}