using Collaboration.Application.Commands.Vacancy;
using Collaboration.Application.Queries.Vacancy;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateTeamVacancyCommandValidator : AbstractValidator<CreateTeamVacancyCommand>
{
    public CreateTeamVacancyCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.Requirements)
            .MaximumLength(2000).WithMessage("Requirements cannot exceed 2000 characters.");
    }
}

public class UpdateTeamVacancyCommandValidator : AbstractValidator<UpdateTeamVacancyCommand>
{
    public UpdateTeamVacancyCommandValidator()
    {
        RuleFor(x => x.VacancyId)
            .NotEmpty().WithMessage("VacancyId is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Requirements)
            .MaximumLength(2000).WithMessage("Requirements cannot exceed 2000 characters.")
            .When(x => x.Requirements is not null);
    }
}

public class CloseTeamVacancyCommandValidator : AbstractValidator<CloseTeamVacancyCommand>
{
    public CloseTeamVacancyCommandValidator()
    {
        RuleFor(x => x.VacancyId)
            .NotEmpty().WithMessage("VacancyId is required.");
    }
}

public class ApplyToVacancyCommandValidator : AbstractValidator<ApplyToVacancyCommand>
{
    public ApplyToVacancyCommandValidator()
    {
        RuleFor(x => x.VacancyId)
            .NotEmpty().WithMessage("VacancyId is required.");

        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters.");
    }
}

public class AcceptVacancyApplicationCommandValidator : AbstractValidator<AcceptVacancyApplicationCommand>
{
    public AcceptVacancyApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ApplicationId is required.");
    }
}

public class RejectVacancyApplicationCommandValidator : AbstractValidator<RejectVacancyApplicationCommand>
{
    public RejectVacancyApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ApplicationId is required.");
    }
}

public class CancelVacancyApplicationCommandValidator : AbstractValidator<CancelVacancyApplicationCommand>
{
    public CancelVacancyApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ApplicationId is required.");
    }
}

public class SearchVacanciesQueryValidator : AbstractValidator<SearchVacanciesQuery>
{
    public SearchVacanciesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be >= 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");

        RuleFor(x => x.Query)
            .MaximumLength(200).WithMessage("Query cannot exceed 200 characters.");
    }
}

public class GetTeamVacanciesQueryValidator : AbstractValidator<GetTeamVacanciesQuery>
{
    public GetTeamVacanciesQueryValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be >= 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

public class GetVacancyApplicationsQueryValidator : AbstractValidator<GetVacancyApplicationsQuery>
{
    public GetVacancyApplicationsQueryValidator()
    {
        RuleFor(x => x.VacancyId)
            .NotEmpty().WithMessage("VacancyId is required.");
    }
}

// GetMyVacancyApplicationsQuery has nothing to validate: it has no input, because the
// applicant is taken from the token.