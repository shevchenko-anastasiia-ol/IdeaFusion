using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface ITeamVacancyRepository : IGenericRepository<TeamVacancy>
{
    Task<PagedList<TeamVacancy>> GetByTeamIdAsync(
        string teamId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedList<TeamVacancy>> SearchVacanciesAsync(
        string? query, string? category, string? role, string? teamId, bool openOnly,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<List<TeamVacancy>> GetOpenByTeamIdsAsync(
        IEnumerable<string> teamIds, CancellationToken cancellationToken = default);

    // Викликається при зміні даних команди — оновлює snapshot у вакансіях
    Task UpdateTeamSnapshotAsync(
        string teamId, string teamName, string? teamAvatarUrl, CancellationToken cancellationToken = default);
}