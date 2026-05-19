using Collaboration.Application.Interfaces.Commands;

namespace Collaboration.Application.Commands.GroupInvitation;

public class CreateGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string TeamId { get; init; } = default!;
    public string InvitedUserId { get; init; } = default!;
    public string InvitedByUserId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Message { get; init; }
    public int ExpirationDays { get; init; } = 7;
}
 
public class AcceptGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string InvitationId { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string? AvatarUrl { get; init; }
}
 
public class DeclineGroupInvitationCommand : ICommand<Domain.Entities.GroupInvitation>
{
    public string InvitationId { get; init; } = default!;
    public string UserId { get; init; } = default!;
}
 
public class RevokeGroupInvitationCommand : ICommand
{
    public string InvitationId { get; init; } = default!;
    public string UserId { get; init; } = default!;
}