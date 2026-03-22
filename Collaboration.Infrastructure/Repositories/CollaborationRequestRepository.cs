using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class CollaborationRequestRepository : GenericRepository<CollaborationRequest>, ICollaborationRequestRepository
{
    private readonly IMongoCollection<CollaborationRequest> _collection;
 
    public CollaborationRequestRepository(IMongoDatabase database) : base(database, "CollaborationRequests")
    {
        _collection = database.GetCollection<CollaborationRequest>("CollaborationRequests");
    }
 
    public async Task<IEnumerable<CollaborationRequest>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return [];
 
        var filter = Builders<CollaborationRequest>.Filter.Eq(r => r.TeamId, teamId);
        return await _collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<CollaborationRequest>> GetByFromUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];
 
        var filter = Builders<CollaborationRequest>.Filter.Eq(r => r.FromUserId, userId);
        return await _collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<CollaborationRequest>> GetByStatusAsync(string teamId, CollaborationRequestStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollaborationRequest>.Filter.And(
            Builders<CollaborationRequest>.Filter.Eq(r => r.TeamId, teamId),
            Builders<CollaborationRequest>.Filter.Eq(r => r.Status, status));
 
        return await _collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<bool> HasPendingRequestAsync(string teamId, string fromUserId, string role, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollaborationRequest>.Filter.And(
            Builders<CollaborationRequest>.Filter.Eq(r => r.TeamId, teamId),
            Builders<CollaborationRequest>.Filter.Eq(r => r.FromUserId, fromUserId),
            Builders<CollaborationRequest>.Filter.Eq(r => r.Role, role),
            Builders<CollaborationRequest>.Filter.Eq(r => r.Status, CollaborationRequestStatus.Pending));
 
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
 
    public async Task<PagedList<CollaborationRequest>> GetPagedFilteredAsync(
        string? teamId, string? fromUserId, string? toUserId, string? role,
        CollaborationRequestStatus? status,
        int pageNumber, int pageSize,
        bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<CollaborationRequest>>();
 
        if (!string.IsNullOrWhiteSpace(teamId))
            filters.Add(Builders<CollaborationRequest>.Filter.Eq(r => r.TeamId, teamId));
 
        if (!string.IsNullOrWhiteSpace(fromUserId))
            filters.Add(Builders<CollaborationRequest>.Filter.Eq(r => r.FromUserId, fromUserId));
 
        if (!string.IsNullOrWhiteSpace(toUserId))
            filters.Add(Builders<CollaborationRequest>.Filter.Eq(r => r.ToUserId, toUserId));
 
        if (!string.IsNullOrWhiteSpace(role))
            filters.Add(Builders<CollaborationRequest>.Filter.Eq(r => r.Role, role));
 
        if (status.HasValue)
            filters.Add(Builders<CollaborationRequest>.Filter.Eq(r => r.Status, status.Value));
 
        var combinedFilter = filters.Any()
            ? Builders<CollaborationRequest>.Filter.And(filters)
            : Builders<CollaborationRequest>.Filter.Empty;
 
        var sort = ascending
            ? Builders<CollaborationRequest>.Sort.Ascending(r => r.CreatedAt)
            : Builders<CollaborationRequest>.Sort.Descending(r => r.CreatedAt);
 
        return await PagedList<CollaborationRequest>.ToPagedListAsync(
            _collection.Find(combinedFilter).Sort(sort),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<PagedList<CollaborationRequest>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(cursor)
            ? Builders<CollaborationRequest>.Filter.Empty
            : Builders<CollaborationRequest>.Filter.Gt(r => r.Id, cursor);
 
        var items = await _collection.Find(filter)
            .Sort(Builders<CollaborationRequest>.Sort.Ascending(r => r.Id))
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
 
        return PagedList<CollaborationRequest>.FromCursor(items, pageSize);
    }
}