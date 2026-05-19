using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.Team;

public class CreateTeamCommandHandler : ICommandHandler<CreateTeamCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public CreateTeamCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _teamRepository.ExistsByNameAsync(request.Name, cancellationToken: cancellationToken);
        if (nameExists)
            throw new DomainException($"Team with name '{request.Name}' already exists.");
 
        var owner = new UserSnapshot(request.UserId, request.Username, request.AvatarUrl);
        var team = new Domain.Entities.Team(request.Name, request.Description, request.Category, request.Tags, owner, request.TeamAvatarUrl);
 
        await _teamRepository.CreateAsync(team, cancellationToken);
        return team;
    }
}
 
public class UpdateTeamCommandHandler : ICommandHandler<UpdateTeamCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public UpdateTeamCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var nameExists = await _teamRepository.ExistsByNameAsync(request.Name, request.TeamId, cancellationToken);
        if (nameExists)
            throw new DomainException($"Team with name '{request.Name}' already exists.");
 
        team.Update(request.Name, request.Description, request.Category, request.Tags, request.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class DeleteTeamCommandHandler : ICommandHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
 
    public DeleteTeamCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Unit> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        team.MarkAsDeleted();
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return Unit.Value;
    }
}
 
public class SetTeamStatusCommandHandler : ICommandHandler<SetTeamStatusCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public SetTeamStatusCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(SetTeamStatusCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        team.SetStatus(request.Status, request.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class AddTeamMemberCommandHandler : ICommandHandler<AddTeamMemberCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public AddTeamMemberCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        var userSnapshot = new UserSnapshot(request.UserId, request.Username, request.AvatarUrl);
        team.AddMember(userSnapshot, request.Role);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class RemoveTeamMemberCommandHandler : ICommandHandler<RemoveTeamMemberCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public RemoveTeamMemberCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        team.RemoveMember(request.UserId, request.RequestedByUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class AddRequiredRoleCommandHandler : ICommandHandler<AddRequiredRoleCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
 
    public AddRequiredRoleCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }
 
    public async Task<Domain.Entities.Team> Handle(AddRequiredRoleCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        team.AddRequiredRole(request.Role, request.Description, request.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}

public class SetTeamAvatarUrlCommandHandler : ICommandHandler<SetTeamAvatarUrlCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;

    public SetTeamAvatarUrlCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<Domain.Entities.Team> Handle(SetTeamAvatarUrlCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.SetAvatarUrl(request.AvatarUrl, request.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}