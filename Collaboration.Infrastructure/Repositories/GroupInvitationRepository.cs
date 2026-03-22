using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class GroupInvitationRepository : GenericRepository<GroupInvitation>, IGroupInvitationRepository
{
    private readonly IMongoCollection<GroupInvitation> _collection;
 
    public GroupInvitationRepository(IMongoDatabase database) : base(database, "GroupInvitations")
    {
        _collection = database.GetCollection<GroupInvitation>("GroupInvitations");
    }
 
    public async Task<IEnumerable<GroupInvitation>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return [];
 
        var filter = Builders<GroupInvitation>.Filter.Eq(i => i.TeamId, teamId);
        return await _collection.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<GroupInvitation>> GetByInvitedUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];
 
        var filter = Builders<GroupInvitation>.Filter.Eq(i => i.InvitedUserId, userId);
        return await _collection.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<GroupInvitation>> GetByStatusAsync(string teamId, InvitationStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GroupInvitation>.Filter.And(
            Builders<GroupInvitation>.Filter.Eq(i => i.TeamId, teamId),
            Builders<GroupInvitation>.Filter.Eq(i => i.Status, status));
 
        return await _collection.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }
 
    public async Task<bool> HasPendingInvitationAsync(string teamId, string invitedUserId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GroupInvitation>.Filter.And(
            Builders<GroupInvitation>.Filter.Eq(i => i.TeamId, teamId),
            Builders<GroupInvitation>.Filter.Eq(i => i.InvitedUserId, invitedUserId),
            Builders<GroupInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<GroupInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow));
 
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
 
    public async Task<IEnumerable<GroupInvitation>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<GroupInvitation>.Filter.And(
            Builders<GroupInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<GroupInvitation>.Filter.Lt(i => i.ExpiresAt, DateTime.UtcNow));
 
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
 
    public async Task<PagedList<GroupInvitation>> GetPagedFilteredAsync(
        string? teamId, string? invitedUserId, string? invitedByUserId, string? role,
        InvitationStatus? status, bool? isExpired,
        int pageNumber, int pageSize,
        bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<GroupInvitation>>();
 
        if (!string.IsNullOrWhiteSpace(teamId))
            filters.Add(Builders<GroupInvitation>.Filter.Eq(i => i.TeamId, teamId));
 
        if (!string.IsNullOrWhiteSpace(invitedUserId))
            filters.Add(Builders<GroupInvitation>.Filter.Eq(i => i.InvitedUserId, invitedUserId));
 
        if (!string.IsNullOrWhiteSpace(invitedByUserId))
            filters.Add(Builders<GroupInvitation>.Filter.Eq(i => i.InvitedByUserId, invitedByUserId));
 
        if (!string.IsNullOrWhiteSpace(role))
            filters.Add(Builders<GroupInvitation>.Filter.Eq(i => i.Role, role));
 
        if (status.HasValue)
            filters.Add(Builders<GroupInvitation>.Filter.Eq(i => i.Status, status.Value));
 
        if (isExpired.HasValue)
        {
            filters.Add(isExpired.Value
                ? Builders<GroupInvitation>.Filter.Lt(i => i.ExpiresAt, DateTime.UtcNow)
                : Builders<GroupInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow));
        }
 
        var combinedFilter = filters.Any()
            ? Builders<GroupInvitation>.Filter.And(filters)
            : Builders<GroupInvitation>.Filter.Empty;
 
        var sort = ascending
            ? Builders<GroupInvitation>.Sort.Ascending(i => i.CreatedAt)
            : Builders<GroupInvitation>.Sort.Descending(i => i.CreatedAt);
 
        return await PagedList<GroupInvitation>.ToPagedListAsync(
            _collection.Find(combinedFilter).Sort(sort),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<PagedList<GroupInvitation>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(cursor)
            ? Builders<GroupInvitation>.Filter.Empty
            : Builders<GroupInvitation>.Filter.Gt(i => i.Id, cursor);
 
        var items = await _collection.Find(filter)
            .Sort(Builders<GroupInvitation>.Sort.Ascending(i => i.Id))
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
 
        return PagedList<GroupInvitation>.FromCursor(items, pageSize);
    }
}