using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface IGroupInvitationRepository : IGenericRepository<GroupInvitation>
{
    // Отримання за контекстом
    Task<IEnumerable<GroupInvitation>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupInvitation>> GetByInvitedUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupInvitation>> GetByStatusAsync(string teamId, InvitationStatus status, CancellationToken cancellationToken = default);
 
    // Перевірки
    Task<bool> HasPendingInvitationAsync(string teamId, string invitedUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupInvitation>> GetExpiredAsync(CancellationToken cancellationToken = default);
 
    // Пагінація з фільтрами
    Task<PagedList<GroupInvitation>> GetPagedFilteredAsync(
        string? teamId,
        string? invitedUserId,
        string? invitedByUserId,
        string? role,
        InvitationStatus? status,
        bool? isExpired,
        int pageNumber,
        int pageSize,
        bool ascending = false,
        CancellationToken cancellationToken = default);
 
    Task<PagedList<GroupInvitation>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);
}