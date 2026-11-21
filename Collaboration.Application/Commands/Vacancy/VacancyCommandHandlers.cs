using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.Vacancy;

public class CreateTeamVacancyCommandHandler
    : ICommandHandler<CreateTeamVacancyCommand, Domain.Entities.TeamVacancy>
{
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public CreateTeamVacancyCommandHandler(
        ITeamVacancyRepository vacancyRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _vacancyRepository = vacancyRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.TeamVacancy> Handle(
        CreateTeamVacancyCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{request.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ManageVacancies);

        var vacancy = new Domain.Entities.TeamVacancy(
            team.Id, team.Name, team.Category, team.Tags, request.Role,
            _caller.UserId, request.Description, request.Requirements, team.AvatarUrl);

        await _vacancyRepository.CreateAsync(vacancy, cancellationToken);

        team.IncrementOpenVacancyCount(_caller.UserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);

        return vacancy;
    }
}

public class UpdateTeamVacancyCommandHandler
    : ICommandHandler<UpdateTeamVacancyCommand, Domain.Entities.TeamVacancy>
{
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public UpdateTeamVacancyCommandHandler(
        ITeamVacancyRepository vacancyRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _vacancyRepository = vacancyRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.TeamVacancy> Handle(
        UpdateTeamVacancyCommand request, CancellationToken cancellationToken)
    {
        var vacancy = await _vacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new DomainException($"Vacancy '{request.VacancyId}' not found.");

        var team = await _teamRepository.GetByIdAsync(vacancy.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{vacancy.TeamId}' not found.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ManageVacancies);

        var oldRole = vacancy.Role;
        vacancy.Update(request.Role, request.Description, request.Requirements, _caller.UserId);
        await _vacancyRepository.UpdateAsync(vacancy, cancellationToken);

        // A renamed vacancy renames the role the team lists, so the two never disagree.
        if (!string.Equals(oldRole, vacancy.Role, StringComparison.Ordinal))
        {
            team.RenameRequiredRole(oldRole, vacancy.Role, _caller.UserId);
            await _teamRepository.UpdateAsync(team, cancellationToken);
        }

        return vacancy;
    }
}

public class CloseTeamVacancyCommandHandler
    : ICommandHandler<CloseTeamVacancyCommand, Domain.Entities.TeamVacancy>
{
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public CloseTeamVacancyCommandHandler(
        ITeamVacancyRepository vacancyRepository,
        IVacancyApplicationRepository applicationRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _vacancyRepository = vacancyRepository;
        _applicationRepository = applicationRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.TeamVacancy> Handle(
        CloseTeamVacancyCommand request, CancellationToken cancellationToken)
    {
        var vacancy = await _vacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new DomainException($"Vacancy '{request.VacancyId}' not found.");

        var team = await _teamRepository.GetByIdAsync(vacancy.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{vacancy.TeamId}' not found.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ManageVacancies);

        if (vacancy.Status == TeamVacancyStatus.Closed)
            return vacancy;

        vacancy.Close(_caller.UserId);
        team.DecrementOpenVacancyCount(_caller.UserId);

        var pending = (await _applicationRepository.GetByVacancyIdAsync(
            vacancy.Id, cancellationToken))
            .Where(a => a.Status == VacancyApplicationStatus.Pending)
            .ToList();

        foreach (var application in pending)
        {
            application.Reject(_caller.UserId);
            await _applicationRepository.UpdateAsync(application, cancellationToken);
        }

        await _vacancyRepository.UpdateAsync(vacancy, cancellationToken);
        await _teamRepository.UpdateAsync(team, cancellationToken);

        return vacancy;
    }
}

public class ApplyToVacancyCommandHandler
    : ICommandHandler<ApplyToVacancyCommand, Domain.Entities.VacancyApplication>
{
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserSnapshotService _userSnapshotService;
    private readonly ICallerContext _caller;

    public ApplyToVacancyCommandHandler(
        ITeamVacancyRepository vacancyRepository,
        IVacancyApplicationRepository applicationRepository,
        ITeamRepository teamRepository,
        IUserSnapshotService userSnapshotService,
        ICallerContext caller)
    {
        _vacancyRepository = vacancyRepository;
        _applicationRepository = applicationRepository;
        _teamRepository = teamRepository;
        _userSnapshotService = userSnapshotService;
        _caller = caller;
    }

    public async Task<Domain.Entities.VacancyApplication> Handle(
        ApplyToVacancyCommand request, CancellationToken cancellationToken)
    {
        var vacancy = await _vacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new DomainException($"Vacancy '{request.VacancyId}' not found.");

        if (vacancy.IsDeleted || vacancy.Status != TeamVacancyStatus.Open)
            throw new DomainException("Vacancy is not open for applications.");

        var team = await _teamRepository.GetByIdAsync(vacancy.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{vacancy.TeamId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        if (team.Members.Any(m => m.UserId == _caller.UserId))
            throw new DomainException("You are already a member of this team.");

        if (await _applicationRepository.ExistsPendingAsync(request.VacancyId, _caller.UserId, cancellationToken))
            throw new DomainException("You have already applied to this vacancy.");

        // The applicant is the caller. Identity is consulted only to fill in the avatar,
        // which lives on the profile and not in the token.
        var applicant = _caller.ToSnapshot();
        if (applicant.AvatarUrl is null)
        {
            var (_, avatarUrl) = await _userSnapshotService.GetUserSnapshotDataAsync(
                _caller.UserId, cancellationToken);
            applicant = new UserSnapshot(applicant.UserId, applicant.Username, avatarUrl);
        }

        var application = new Domain.Entities.VacancyApplication(
            vacancy.Id, team.Id, applicant, vacancy.Role, request.Message);

        await _applicationRepository.CreateAsync(application, cancellationToken);
        return application;
    }
}

public class AcceptVacancyApplicationCommandHandler
    : ICommandHandler<AcceptVacancyApplicationCommand, Domain.Entities.VacancyApplication>
{
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public AcceptVacancyApplicationCommandHandler(
        IVacancyApplicationRepository applicationRepository,
        ITeamVacancyRepository vacancyRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _applicationRepository = applicationRepository;
        _vacancyRepository = vacancyRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.VacancyApplication> Handle(
        AcceptVacancyApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException($"Application '{request.ApplicationId}' not found.");

        if (application.Status != VacancyApplicationStatus.Pending)
            throw new DomainException($"Application is already {application.Status}.");

        var vacancy = await _vacancyRepository.GetByIdAsync(application.VacancyId, cancellationToken)
            ?? throw new DomainException($"Vacancy '{application.VacancyId}' not found.");

        var team = await _teamRepository.GetByIdAsync(application.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{application.TeamId}' not found.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ReviewApplications);

        if (vacancy.Status == TeamVacancyStatus.Closed)
            throw new DomainException("Vacancy is already closed.");

        if (!team.Members.Any(m => m.UserId == application.Applicant.UserId))
            team.Join(application.Applicant, application.Role);

        application.Accept(_caller.UserId);
        vacancy.Close(_caller.UserId);
        team.DecrementOpenVacancyCount(_caller.UserId);

        var otherPending = (await _applicationRepository.GetByVacancyIdAsync(
            vacancy.Id, cancellationToken))
            .Where(a => a.Id != application.Id && a.Status == VacancyApplicationStatus.Pending)
            .ToList();

        foreach (var other in otherPending)
        {
            other.Reject(_caller.UserId);
            await _applicationRepository.UpdateAsync(other, cancellationToken);
        }

        await Task.WhenAll(
            _applicationRepository.UpdateAsync(application, cancellationToken),
            _vacancyRepository.UpdateAsync(vacancy, cancellationToken),
            _teamRepository.UpdateAsync(team, cancellationToken));

        return application;
    }
}

public class RejectVacancyApplicationCommandHandler
    : ICommandHandler<RejectVacancyApplicationCommand, Domain.Entities.VacancyApplication>
{
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public RejectVacancyApplicationCommandHandler(
        IVacancyApplicationRepository applicationRepository,
        ITeamVacancyRepository vacancyRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _applicationRepository = applicationRepository;
        _vacancyRepository = vacancyRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.VacancyApplication> Handle(
        RejectVacancyApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException($"Application '{request.ApplicationId}' not found.");

        if (application.Status != VacancyApplicationStatus.Pending)
            throw new DomainException($"Application is already {application.Status}.");

        var vacancy = await _vacancyRepository.GetByIdAsync(application.VacancyId, cancellationToken)
            ?? throw new DomainException($"Vacancy '{application.VacancyId}' not found.");

        var team = await _teamRepository.GetByIdAsync(application.TeamId, cancellationToken)
            ?? throw new DomainException($"Team '{application.TeamId}' not found.");

        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ReviewApplications);

        application.Reject(_caller.UserId);
        await _applicationRepository.UpdateAsync(application, cancellationToken);

        return application;
    }
}

public class CancelVacancyApplicationCommandHandler : ICommandHandler<CancelVacancyApplicationCommand>
{
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ICallerContext _caller;

    public CancelVacancyApplicationCommandHandler(
        IVacancyApplicationRepository applicationRepository,
        ICallerContext caller)
    {
        _applicationRepository = applicationRepository;
        _caller = caller;
    }

    public async Task<Unit> Handle(CancelVacancyApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException($"Application '{request.ApplicationId}' not found.");

        if (application.Applicant.UserId != _caller.UserId)
            throw new ForbiddenException("Only the applicant can cancel their application.");

        application.Cancel(_caller.UserId);
        await _applicationRepository.UpdateAsync(application, cancellationToken);
        return Unit.Value;
    }
}