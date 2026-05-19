using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.GroupInvitation;

public class CreateGroupInvitationCommandHandler
    : ICommandHandler<CreateGroupInvitationCommand, Domain.Entities.GroupInvitation>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
 
    public CreateGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        CreateGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var alreadyMember = await _teamRepository.IsUserMemberAsync(
            request.TeamId, request.InvitedUserId, cancellationToken);
        if (alreadyMember)
            throw new DomainException("User is already a member of this team.");
 
        var hasPending = await _invitationRepository.HasPendingInvitationAsync(
            request.TeamId, request.InvitedUserId, cancellationToken);
        if (hasPending)
            throw new DomainException("A pending invitation for this user already exists.");
 
        var invitation = new Domain.Entities.GroupInvitation(
            request.TeamId, request.InvitedUserId, request.InvitedByUserId,
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
 
    public AcceptGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        AcceptGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        if (invitation.InvitedUserId != request.UserId)
            throw new DomainException("Only the invited user can accept the invitation.");
 
        var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{invitation.TeamId}' not found.");

        invitation.Accept(request.UserId);

        var userSnapshot = new UserSnapshot(request.UserId, request.Username, request.AvatarUrl);
        team.AddMember(userSnapshot, invitation.Role);

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
 
    public DeclineGroupInvitationCommandHandler(IGroupInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }
 
    public async Task<Domain.Entities.GroupInvitation> Handle(
        DeclineGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        if (invitation.InvitedUserId != request.UserId)
            throw new DomainException("Only the invited user can decline the invitation.");
 
        invitation.Decline(request.UserId);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        return invitation;
    }
}
 
public class RevokeGroupInvitationCommandHandler : ICommandHandler<RevokeGroupInvitationCommand>
{
    private readonly IGroupInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
 
    public RevokeGroupInvitationCommandHandler(
        IGroupInvitationRepository invitationRepository,
        ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Unit> Handle(RevokeGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException($"Invitation '{request.InvitationId}' not found.");
 
        var isMember = await _teamRepository.IsUserMemberAsync(
            invitation.TeamId, request.UserId, cancellationToken);
        if (!isMember)
            throw new DomainException("Only a team member can revoke an invitation.");
 
        invitation.Revoke(request.UserId);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Unit.Value;
    }
}