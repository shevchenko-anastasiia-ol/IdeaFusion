using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Commands.Team;

// None of these commands carry a user id. The caller is whoever holds the token, and
// the handler reads it from ICallerContext, so a request body cannot claim to be
// somebody else. The one exception is a command that names a *target* member, which is
// a subject of the action rather than the actor performing it.

public class CreateTeamCommand : ICommand<Domain.Entities.Team>
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Category { get; init; } = default!;
    public List<string> Tags { get; init; } = [];
    public string? TeamAvatarUrl { get; init; }
}
 
public class UpdateTeamCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Category { get; init; } = default!;
    public List<string> Tags { get; init; } = [];
}
 
public class DeleteTeamCommand : ICommand
{
    public string TeamId { get; init; } = default!;
}
 
public class SetTeamStatusCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public TeamStatus Status { get; init; }
}
 
public class AddTeamMemberCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;

    // The member being added. Not the caller: the actor is the token.
    public string MemberUserId { get; init; } = default!;
    public string? MemberUsername { get; init; }
    public string? MemberAvatarUrl { get; init; }
    public string Role { get; init; } = TeamRole.Member;
}
 
public class RemoveTeamMemberCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;

    // The member being removed, not the caller.
    public string MemberUserId { get; init; } = default!;
}
 
public class AddRequiredRoleCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Description { get; init; }
}

public class SetTeamAvatarUrlCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string? AvatarUrl { get; init; }
}

public class UpdateTeamMemberPermissionsCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;

    // The member whose privileges are being rewritten, not the caller.
    public string TargetMemberId { get; init; } = default!;
    public List<string> Permissions { get; init; } = [];
}

/// <summary>
/// Moves a member between Leader and Member. Administrator-only: the endpoint that
/// dispatches this is the single place a team role changes hands, which is what stops a
/// team from quietly promoting its own members.
/// </summary>
public class SetTeamMemberRoleCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string TargetMemberId { get; init; } = default!;
    public string Role { get; init; } = TeamRole.Member;
}
