using Collaboration.Application.DTOs;
using Collaboration.Application.Interfaces.Queries;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.Vacancy;

public class GetTeamVacancyByIdQueryHandler
    : IQueryHandler<GetTeamVacancyByIdQuery, Domain.Entities.TeamVacancy>
{
    private readonly ITeamVacancyRepository _vacancyRepository;

    public GetTeamVacancyByIdQueryHandler(ITeamVacancyRepository vacancyRepository)
    {
        _vacancyRepository = vacancyRepository;
    }

    public async Task<Domain.Entities.TeamVacancy> Handle(
        GetTeamVacancyByIdQuery request, CancellationToken cancellationToken)
    {
        return await _vacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new NotFoundException($"Vacancy '{request.VacancyId}' not found.");
    }
}

public class GetTeamVacanciesQueryHandler
    : IQueryHandler<GetTeamVacanciesQuery, PagedList<Domain.Entities.TeamVacancy>>
{
    private readonly ITeamVacancyRepository _vacancyRepository;

    public GetTeamVacanciesQueryHandler(ITeamVacancyRepository vacancyRepository)
    {
        _vacancyRepository = vacancyRepository;
    }

    public async Task<PagedList<Domain.Entities.TeamVacancy>> Handle(
        GetTeamVacanciesQuery request, CancellationToken cancellationToken)
    {
        return await _vacancyRepository.GetByTeamIdAsync(
            request.TeamId, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class SearchVacanciesQueryHandler
    : IQueryHandler<SearchVacanciesQuery, PagedList<Domain.Entities.TeamVacancy>>
{
    private readonly ITeamVacancyRepository _vacancyRepository;

    public SearchVacanciesQueryHandler(ITeamVacancyRepository vacancyRepository)
    {
        _vacancyRepository = vacancyRepository;
    }

    public async Task<PagedList<Domain.Entities.TeamVacancy>> Handle(
        SearchVacanciesQuery request, CancellationToken cancellationToken)
    {
        return await _vacancyRepository.SearchVacanciesAsync(
            request.Query, request.Category, request.Role, request.TeamId, request.OpenOnly,
            request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetVacancyApplicationsQueryHandler
    : IQueryHandler<GetVacancyApplicationsQuery, IEnumerable<VacancyApplicationDto>>
{
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamVacancyRepository _vacancyRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public GetVacancyApplicationsQueryHandler(
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

    public async Task<IEnumerable<VacancyApplicationDto>> Handle(
        GetVacancyApplicationsQuery request, CancellationToken cancellationToken)
    {
        var vacancy = await _vacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new NotFoundException($"Vacancy '{request.VacancyId}' not found.");

        var team = await _teamRepository.GetByIdAsync(vacancy.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Team '{vacancy.TeamId}' not found.");

        // The reviewer is the token's subject. Reading the applicant list is a privilege
        // of running the team, so it is checked as one rather than as a role string.
        team.EnsurePermission(_caller.UserId, Domain.Entities.TeamPermission.ReviewApplications);

        var applications = await _applicationRepository.GetByVacancyIdAsync(
            request.VacancyId, cancellationToken);

        return applications.Select(a => VacancyApplicationDtoFactory.From(a, team.Name)).ToList();
    }
}

public class GetMyVacancyApplicationsQueryHandler
    : IQueryHandler<GetMyVacancyApplicationsQuery, IEnumerable<VacancyApplicationDto>>
{
    private readonly IVacancyApplicationRepository _applicationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public GetMyVacancyApplicationsQueryHandler(
        IVacancyApplicationRepository applicationRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _applicationRepository = applicationRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<IEnumerable<VacancyApplicationDto>> Handle(
        GetMyVacancyApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = (await _applicationRepository.GetByApplicantUserIdAsync(
            _caller.UserId, cancellationToken)).ToList();

        var teamIds = applications.Select(a => a.TeamId).Distinct().ToList();
        var teams = await _teamRepository.FindAsync(t => teamIds.Contains(t.Id), cancellationToken);
        var teamNames = teams.ToDictionary(t => t.Id, t => t.Name);

        return applications.Select(a =>
        {
            teamNames.TryGetValue(a.TeamId, out var teamName);
            return VacancyApplicationDtoFactory.From(a, teamName ?? string.Empty);
        }).ToList();
    }
}