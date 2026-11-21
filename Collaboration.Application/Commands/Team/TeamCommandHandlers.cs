using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.Team;

// Every handler below reads the actor from ICallerContext. A command that used to carry
// a userId would let the caller name somebody else; that parameter is gone from the
// request shape, not merely ignored.

public class CreateTeamCommandHandler : ICommandHandler<CreateTeamCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public CreateTeamCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.Team> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _teamRepository.ExistsByNameAsync(request.Name, cancellationToken: cancellationToken);
        if (nameExists)
            throw new DomainException($"Team with name '{request.Name}' already exists.");
 
        // The creator is the owner, and it is the token that says so.
        var owner = _caller.ToSnapshot();
        var team = new Domain.Entities.Team(request.Name, request.Description, request.Category, request.Tags, owner, request.TeamAvatarUrl);
 
        await _teamRepository.CreateAsync(team, cancellationToken);
        return team;
    }
}
 
public class UpdateTeamCommandHandler : ICommandHandler<UpdateTeamCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public UpdateTeamCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
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

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditTeam);

        team.Update(request.Name, request.Description, request.Category, request.Tags, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class DeleteTeamCommandHandler : ICommandHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public DeleteTeamCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Unit> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        // Owner only, not "has EditTeam": ending a team is not an edit.
        team.EnsureOwner(_caller.UserId);

        team.MarkAsDeleted();
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return Unit.Value;
    }
}
 
public class SetTeamStatusCommandHandler : ICommandHandler<SetTeamStatusCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public SetTeamStatusCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.Team> Handle(SetTeamStatusCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditTeam);

        team.SetStatus(request.Status, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class AddTeamMemberCommandHandler : ICommandHandler<AddTeamMemberCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserSnapshotService _users;
    private readonly ICallerContext _caller;
 
    public AddTeamMemberCommandHandler(
        ITeamRepository teamRepository,
        IUserSnapshotService users,
        ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _users = users;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.Team> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");
 
        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");
 
        // The target's profile is resolved from Identity rather than taken from the body,
        // so an invite cannot invent a name for a subject that does not have one.
        var (directoryName, directoryAvatar) =
            await _users.GetUserSnapshotDataAsync(request.MemberUserId, cancellationToken);

        var userSnapshot = new UserSnapshot(
            request.MemberUserId,
            // The directory's name wins; the body may only fill a gap the directory left.
            string.IsNullOrWhiteSpace(directoryName) ? request.MemberUsername ?? request.MemberUserId : directoryName,
            directoryAvatar ?? request.MemberAvatarUrl);

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.InviteMembers);
        team.AddMember(userSnapshot, request.Role);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class RemoveTeamMemberCommandHandler : ICommandHandler<RemoveTeamMemberCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;
 
    public RemoveTeamMemberCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.Team> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        // A member may always remove themselves; removing somebody else needs the
        // privilege. Checking the target against the caller here is what stops a caller
        // passing their own id to make "remove member" behave as "leave team".
        var isSelf = string.Equals(request.MemberUserId, _caller.UserId, StringComparison.Ordinal);
        if (!isSelf)
        {
            team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.RemoveMembers);
        }

        team.RemoveMember(request.MemberUserId, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
 
public class AddRequiredRoleCommandHandler : ICommandHandler<AddRequiredRoleCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ICallerContext _caller;
 
    public AddRequiredRoleCommandHandler(
        ITeamRepository teamRepository, ITeamVacancyRepository vacancyRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _vacancyRepository = vacancyRepository;
        _caller = caller;
    }
 
    public async Task<Domain.Entities.Team> Handle(AddRequiredRoleCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditTeam);

        team.AddRequiredRole(request.Role, request.Description, _caller.UserId);

        // A role the team is looking for is announced as a vacancy straight away, so it
        // shows up in the catalogue and in search. The team edits it later (description,
        // requirements) like any other vacancy. One open vacancy per role is enough.
        if (team.HasPermission(_caller.UserId, Domain.Entities.TeamPermission.ManageVacancies))
        {
            var role = request.Role.Trim();
            var open = await _vacancyRepository.GetOpenByTeamIdsAsync([team.Id], cancellationToken);
            if (!open.Any(v => string.Equals(v.Role, role, StringComparison.OrdinalIgnoreCase)))
            {
                var vacancy = new Domain.Entities.TeamVacancy(
                    team.Id, team.Name, team.Category, team.Tags, role,
                    _caller.UserId, request.Description, null, team.AvatarUrl);
                await _vacancyRepository.CreateAsync(vacancy, cancellationToken);
                team.IncrementOpenVacancyCount(_caller.UserId);
            }
        }

        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}

public class SetTeamAvatarUrlCommandHandler : ICommandHandler<SetTeamAvatarUrlCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public SetTeamAvatarUrlCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.Team> Handle(SetTeamAvatarUrlCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.EditTeam);

        team.SetAvatarUrl(request.AvatarUrl, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}

public class UpdateTeamMemberPermissionsCommandHandler
    : ICommandHandler<UpdateTeamMemberPermissionsCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public UpdateTeamMemberPermissionsCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.Team> Handle(
        UpdateTeamMemberPermissionsCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.SetMemberPermissions(request.TargetMemberId, request.Permissions, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}

public class SetTeamMemberRoleCommandHandler
    : ICommandHandler<SetTeamMemberRoleCommand, Domain.Entities.Team>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public SetTeamMemberRoleCommandHandler(ITeamRepository teamRepository, ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.Team> Handle(
        SetTeamMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // Checked in the handler as well as on the [Authorize] policy: this is the
        // method that actually changes somebody's rank, so it does not rely on the
        // route being wired correctly.
        if (!_caller.IsAdministrator)
            throw new ForbiddenException("Only an administrator can change a member's role.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        if (request.Role == Domain.Entities.TeamRole.Owner)
            throw new DomainException("A team's owner cannot be reassigned.");

        team.SetMemberRole(request.TargetMemberId, request.Role, _caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return team;
    }
}
