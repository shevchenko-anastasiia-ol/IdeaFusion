using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Commands.Team;

public class CreateTeamCommand : ICommand<Domain.Entities.Team>
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Category { get; init; } = default!;
    public List<string> Tags { get; init; } = [];
    public string UserId { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string? AvatarUrl { get; init; }
}
 
public class UpdateTeamCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Category { get; init; } = default!;
    public List<string> Tags { get; init; } = [];
    public string UserId { get; init; } = default!;
}
 
public class DeleteTeamCommand : ICommand
{
    public string TeamId { get; init; } = default!;
    public string UserId { get; init; } = default!;
}
 
public class SetTeamStatusCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public TeamStatus Status { get; init; }
    public string UserId { get; init; } = default!;
}
 
public class AddTeamMemberCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = default!;
    public string RequestedByUserId { get; init; } = default!;
}
 
public class RemoveTeamMemberCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string RequestedByUserId { get; init; } = default!;
}
 
public class AddRequiredRoleCommand : ICommand<Domain.Entities.Team>
{
    public string TeamId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Description { get; init; }
    public string UserId { get; init; } = default!;
}
