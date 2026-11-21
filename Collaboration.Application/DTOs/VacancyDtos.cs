using Collaboration.Domain.Entities;

namespace Collaboration.Application.DTOs;

public record VacancySummaryDto(
    string Id,
    string TeamId,
    string TeamName,
    string? TeamAvatarUrl,
    string Category,
    List<string> Tags,
    string Role,
    string? Description,
    string? Requirements,
    TeamVacancyStatus Status,
    DateTime CreatedAt);

public record VacancyApplicationDto(
    string Id,
    string VacancyId,
    string TeamId,
    string TeamName,
    string ApplicantUserId,
    string ApplicantUsername,
    string? ApplicantAvatarUrl,
    string Role,
    string? Message,
    VacancyApplicationStatus Status,
    DateTime? ResolvedAt,
    DateTime CreatedAt);

public record VacancyPageDto(
    List<VacancySummaryDto> Items,
    bool HasMore,
    long TotalCount,
    int PageNumber,
    int PageSize)
{
    public static VacancyPageDto From(
        Collaboration.Domain.Common.Helpers.PagedList<Domain.Entities.TeamVacancy> page)
    {
        var items = page.Select(VacancySummaryDtoFactory.From).ToList();
        return new VacancyPageDto(
            items,
            page.CurrentPage * page.PageSize < page.TotalCount,
            page.TotalCount,
            page.CurrentPage,
            page.PageSize);
    }
}

public static class VacancySummaryDtoFactory
{
    public static VacancySummaryDto From(Domain.Entities.TeamVacancy vacancy) => new(
        vacancy.Id,
        vacancy.TeamId,
        vacancy.TeamName,
        vacancy.TeamAvatarUrl,
        vacancy.Category,
        vacancy.Tags,
        vacancy.Role,
        vacancy.Description,
        vacancy.Requirements,
        vacancy.Status,
        vacancy.CreatedAt);
}

public static class VacancyApplicationDtoFactory
{
    public static VacancyApplicationDto From(
        Domain.Entities.VacancyApplication application, string teamName) => new(
        application.Id,
        application.VacancyId,
        application.TeamId,
        teamName,
        application.Applicant.UserId,
        application.Applicant.Username,
        application.Applicant.AvatarUrl,
        application.Role,
        application.Message,
        application.Status,
        application.ResolvedAt,
        application.CreatedAt);
}