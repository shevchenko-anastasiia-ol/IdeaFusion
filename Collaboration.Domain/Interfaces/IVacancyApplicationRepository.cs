using Collaboration.Domain.Common;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface IVacancyApplicationRepository : IGenericRepository<VacancyApplication>
{
    Task<IEnumerable<VacancyApplication>> GetByVacancyIdAsync(
        string vacancyId, CancellationToken cancellationToken = default);

    Task<IEnumerable<VacancyApplication>> GetByApplicantUserIdAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingAsync(
        string vacancyId, string userId, CancellationToken cancellationToken = default);

    // Викликається при зміні користувача — оновлює snapshot заявника
    Task UpdateApplicantSnapshotAsync(
        string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default);
}