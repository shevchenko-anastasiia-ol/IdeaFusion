using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using MediatR;

namespace Collaboration.Application.Commands.TeamPost;

public class CreateTeamPostCommandHandler : ICommandHandler<CreateTeamPostCommand, Domain.Entities.TeamPost>
{
    private readonly ITeamPostRepository _teamPostRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public CreateTeamPostCommandHandler(
        ITeamPostRepository teamPostRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _teamPostRepository = teamPostRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.TeamPost> Handle(CreateTeamPostCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var isMember = await _teamRepository.IsUserMemberAsync(request.TeamId, _caller.UserId, cancellationToken);
        if (!isMember)
            throw new ForbiddenException("Only team members can publish posts on behalf of the team.");
 
        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.PublishPosts);
 
        var alreadyExists = await _teamPostRepository.ExistsByPostIdAsync(request.PostId, request.TeamId, cancellationToken);
        if (alreadyExists)
            throw new DomainException($"Post '{request.PostId}' is already linked to this team.");
 
        // The author of record is the caller, taken from the token, so a post can never
        // be attributed to a member who did not write it.
        var teamPost = new Domain.Entities.TeamPost(
            request.PostId, request.TeamId, _caller.ToSnapshot(), request.Title);
 
        await _teamPostRepository.CreateAsync(teamPost, cancellationToken);
        return teamPost;
    }
}
 
public class UpdateTeamPostTitleCommandHandler : ICommandHandler<UpdateTeamPostTitleCommand, Domain.Entities.TeamPost>
{
    private readonly ITeamPostRepository _teamPostRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public UpdateTeamPostTitleCommandHandler(
        ITeamPostRepository teamPostRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _teamPostRepository = teamPostRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.TeamPost> Handle(UpdateTeamPostTitleCommand request, CancellationToken cancellationToken)
    {
        var teamPost = await _teamPostRepository.GetByPostIdAsync(request.PostId, cancellationToken)
            ?? throw new DomainException($"TeamPost with PostId '{request.PostId}' not found.");
  
        if (teamPost.TeamId != request.TeamId)
            throw new DomainException("Post does not belong to this team.");

        if (teamPost.IsDeleted)
            throw new DomainException("TeamPost is deleted.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        var isAuthor = teamPost.Author.UserId == _caller.UserId;
        if (!isAuthor)
            team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditPosts);

        teamPost.UpdateTitle(request.Title, _caller.UserId);
        await _teamPostRepository.UpdateAsync(teamPost, cancellationToken);
        return teamPost;
    }
}
 
public class DeleteTeamPostCommandHandler : ICommandHandler<DeleteTeamPostCommand>
{
    private readonly ITeamPostRepository _teamPostRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public DeleteTeamPostCommandHandler(
        ITeamPostRepository teamPostRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _teamPostRepository = teamPostRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Unit> Handle(DeleteTeamPostCommand request, CancellationToken cancellationToken)
    {
        var teamPost = await _teamPostRepository.GetByPostIdAsync(request.PostId, cancellationToken)
            ?? throw new DomainException($"TeamPost with PostId '{request.PostId}' not found.");
 
        if (teamPost.TeamId != request.TeamId)
            throw new DomainException("Post does not belong to this team.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        var isAuthor = teamPost.Author.UserId == _caller.UserId;
        if (!isAuthor && !_caller.IsModerator)
            team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditPosts);

        teamPost.MarkAsDeleted();
        await _teamPostRepository.UpdateAsync(teamPost, cancellationToken);
        return Unit.Value;
    }
}
