using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface ITeamPostRepository : IGenericRepository<TeamPost>
{
    // Отримання за контекстом
    Task<TeamPost?> GetByPostIdAsync(string postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamPost>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamPost>> GetByAuthorIdAsync(string userId, CancellationToken cancellationToken = default);
 
    // Перевірки
    Task<bool> ExistsByPostIdAsync(string postId, string teamId, CancellationToken cancellationToken = default);
 
    // Пагінація
    Task<PagedList<TeamPost>> GetByTeamPagedAsync(string teamId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedList<TeamPost>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);
 
    // Синхронізація snapshot через RabbitMQ подію UserUpdated
    Task UpdateAuthorSnapshotAsync(string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default);
}