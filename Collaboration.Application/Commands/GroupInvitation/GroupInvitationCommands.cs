using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Commands.GroupInvitation;

// InvitedUserId survives here as the *subject* of the invitation, which is what an
// invitation is. InvitedByUserId and the UserId on accept/decline/revoke are gone: those
// are the inviter and the actor, and both come from the token.

public class CreateGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string TeamId { get; init; } = default!;
    public string InvitedUserId { get; init; } = default!;
    public string Role { get; init; } = TeamRole.Member;
    public string? Message { get; init; }
    public int ExpirationDays { get; init; } = 7;
}
 
public class AcceptGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string InvitationId { get; init; } = default!;
}
 
public class DeclineGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string InvitationId { get; init; } = default!;
}
 
public class RevokeGroupInvitationCommand : ICommand
{
    public string InvitationId { get; init; } = default!;
}
