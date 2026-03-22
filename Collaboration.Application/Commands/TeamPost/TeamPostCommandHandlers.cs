using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.TeamPost;

public class CreateTeamPostCommandHandler : ICommandHandler<CreateTeamPostCommand, Domain.Entities.TeamPost>
{
    private readonly ITeamPostRepository _teamPostRepository;
    private readonly ITeamRepository _teamRepository;
 
    public CreateTeamPostCommandHandler(
        ITeamPostRepository teamPostRepository,
        ITeamRepository teamRepository)
    {
        _teamPostRepository = teamPostRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.TeamPost> Handle(CreateTeamPostCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var isMember = await _teamRepository.IsUserMemberAsync(request.TeamId, request.AuthorUserId, cancellationToken);
        if (!isMember)
            throw new DomainException("Only team members can publish posts on behalf of the team.");
 
        var alreadyExists = await _teamPostRepository.ExistsByPostIdAsync(request.PostId, request.TeamId, cancellationToken);
        if (alreadyExists)
            throw new DomainException($"Post '{request.PostId}' is already linked to this team.");
 
        var author = new UserSnapshot(request.AuthorUserId, request.AuthorUsername, request.AuthorAvatarUrl);
        var teamPost = new Domain.Entities.TeamPost(request.PostId, request.TeamId, author, request.Title);
 
        await _teamPostRepository.CreateAsync(teamPost, cancellationToken);
        return teamPost;
    }
}
 
public class UpdateTeamPostTitleCommandHandler : ICommandHandler<UpdateTeamPostTitleCommand, Domain.Entities.TeamPost>
{
    private readonly ITeamPostRepository _teamPostRepository;
 
    public UpdateTeamPostTitleCommandHandler(ITeamPostRepository teamPostRepository)
    {
        _teamPostRepository = teamPostRepository;
    }
 
    public async Task<Domain.Entities.TeamPost> Handle(UpdateTeamPostTitleCommand request, CancellationToken cancellationToken)
    {
        var teamPost = await _teamPostRepository.GetByPostIdAsync(request.PostId, cancellationToken)
            ?? throw new DomainException($"TeamPost with PostId '{request.PostId}' not found.");
 
        if (teamPost.TeamId != request.TeamId)
            throw new DomainException("Post does not belong to this team.");
 
        if (teamPost.IsDeleted)
            throw new DomainException("TeamPost is deleted.");
 
        teamPost.UpdateTitle(request.Title, request.UserId);
        await _teamPostRepository.UpdateAsync(teamPost, cancellationToken);
        return teamPost;
    }
}
 
public class DeleteTeamPostCommandHandler : ICommandHandler<DeleteTeamPostCommand>
{
    private readonly ITeamPostRepository _teamPostRepository;
    private readonly ITeamRepository _teamRepository;
 
    public DeleteTeamPostCommandHandler(
        ITeamPostRepository teamPostRepository,
        ITeamRepository teamRepository)
    {
        _teamPostRepository = teamPostRepository;
        _teamRepository = teamRepository;
    }
 
    public async Task<Unit> Handle(DeleteTeamPostCommand request, CancellationToken cancellationToken)
    {
        var teamPost = await _teamPostRepository.GetByPostIdAsync(request.PostId, cancellationToken)
            ?? throw new DomainException($"TeamPost with PostId '{request.PostId}' not found.");
 
        if (teamPost.TeamId != request.TeamId)
            throw new DomainException("Post does not belong to this team.");
 
        var isMember = await _teamRepository.IsUserMemberAsync(request.TeamId, request.UserId, cancellationToken);
        if (!isMember && teamPost.Author.UserId != request.UserId)
            throw new DomainException("Only the author or a team member can delete this post.");
 
        teamPost.MarkAsDeleted();
        await _teamPostRepository.UpdateAsync(teamPost, cancellationToken);
        return Unit.Value;
    }
}