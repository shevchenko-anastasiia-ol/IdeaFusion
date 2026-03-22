using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface ITeamRepository : IGenericRepository<Team>
{
    // Пошук
    Task<IEnumerable<Team>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default);
 
    // Фільтрація
    Task<PagedList<Team>> GetByStatusAsync(TeamStatus status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedList<Team>> GetByCategoryAsync(string category, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Team>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
 
    // За учасником
    Task<IEnumerable<Team>> GetByMemberIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);
 
    // Пагінація
    Task<PagedList<Team>> GetPagedFilteredAsync(
        string? name,
        string? category,
        string? tag,
        TeamStatus? status,
        string? memberId,
        string? requiredRole,
        int pageNumber,
        int pageSize,
        string? orderBy = null,
        bool ascending = false,
        CancellationToken cancellationToken = default);
 
    Task<PagedList<Team>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);
 
    // Синхронізація snapshot через RabbitMQ подію UserUpdated
    Task UpdateMemberSnapshotAsync(string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default);
}