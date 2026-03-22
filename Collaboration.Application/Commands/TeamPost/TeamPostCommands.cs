using Collaboration.Application.Interfaces.Commands;

namespace Collaboration.Application.Commands.TeamPost;

public class CreateTeamPostCommand : ICommand<Domain.Entities.TeamPost>
{
    public string PostId { get; init; } = default!;
    public string TeamId { get; init; } = default!;
    public string AuthorUserId { get; init; } = default!;
    public string AuthorUsername { get; init; } = default!;
    public string? AuthorAvatarUrl { get; init; }
    public string Title { get; init; } = default!;
}
 
public class UpdateTeamPostTitleCommand : ICommand<Domain.Entities.TeamPost>
{
    public string PostId { get; init; } = default!;
    public string TeamId { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string UserId { get; init; } = default!;
}
 
public class DeleteTeamPostCommand : ICommand
{
    public string PostId { get; init; } = default!;
    public string TeamId { get; init; } = default!;
    public string UserId { get; init; } = default!;
}