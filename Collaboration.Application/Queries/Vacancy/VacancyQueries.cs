using Collaboration.Application.DTOs;
using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Queries.Vacancy;

public class GetTeamVacancyByIdQuery : IQuery<Domain.Entities.TeamVacancy>
{
    public string VacancyId { get; init; } = default!;
}

public class GetTeamVacanciesQuery : IQuery<PagedList<Domain.Entities.TeamVacancy>>
{
    public string TeamId { get; init; } = default!;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class SearchVacanciesQuery : IQuery<PagedList<Domain.Entities.TeamVacancy>>
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? Role { get; init; }
    public string? TeamId { get; init; }
    public bool OpenOnly { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// The applicants for one vacancy. Who may ask is decided against the token inside the
/// handler, so this carries no user id.
/// </summary>
public class GetVacancyApplicationsQuery : IQuery<IEnumerable<VacancyApplicationDto>>
{
    public string VacancyId { get; init; } = default!;
}

/// <summary>The caller's own applications. No user id: it is the token's.</summary>
public class GetMyVacancyApplicationsQuery : IQuery<IEnumerable<VacancyApplicationDto>>
{
}