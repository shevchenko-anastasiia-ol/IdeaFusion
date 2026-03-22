using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface ICollaborationRequestRepository : IGenericRepository<CollaborationRequest>
{
    // Отримання за контекстом
    Task<IEnumerable<CollaborationRequest>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CollaborationRequest>> GetByFromUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CollaborationRequest>> GetByStatusAsync(string teamId, CollaborationRequestStatus status, CancellationToken cancellationToken = default);
 
    // Перевірки
    Task<bool> HasPendingRequestAsync(string teamId, string fromUserId, string role, CancellationToken cancellationToken = default);
 
    // Пагінація з фільтрами
    Task<PagedList<CollaborationRequest>> GetPagedFilteredAsync(
        string? teamId,
        string? fromUserId,
        string? toUserId,
        string? role,
        CollaborationRequestStatus? status,
        int pageNumber,
        int pageSize,
        bool ascending = false,
        CancellationToken cancellationToken = default);
 
    Task<PagedList<CollaborationRequest>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);
}