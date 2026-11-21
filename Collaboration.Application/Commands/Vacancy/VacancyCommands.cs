using Collaboration.Application.Interfaces.Commands;

namespace Collaboration.Application.Commands.Vacancy;

// Every UserId below used to name the person acting: the opener, the closer, the
// applicant, the reviewer, the canceller. All five are the caller now.

public class CreateTeamVacancyCommand : ICommand<Domain.Entities.TeamVacancy>
{
    public string TeamId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Description { get; init; }
    public string? Requirements { get; init; }
}

public class UpdateTeamVacancyCommand : ICommand<Domain.Entities.TeamVacancy>
{
    public string VacancyId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Description { get; init; }
    public string? Requirements { get; init; }
}

public class CloseTeamVacancyCommand : ICommand<Domain.Entities.TeamVacancy>
{
    public string VacancyId { get; init; } = default!;
}

public class ApplyToVacancyCommand : ICommand<Domain.Entities.VacancyApplication>
{
    public string VacancyId { get; init; } = default!;
    public string? Message { get; init; }
}

public class AcceptVacancyApplicationCommand : ICommand<Domain.Entities.VacancyApplication>
{
    public string ApplicationId { get; init; } = default!;
}

public class RejectVacancyApplicationCommand : ICommand<Domain.Entities.VacancyApplication>
{
    public string ApplicationId { get; init; } = default!;
}

public class CancelVacancyApplicationCommand : ICommand
{
    public string ApplicationId { get; init; } = default!;
}