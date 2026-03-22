using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    private readonly IMongoCollection<Team> _collection;
 
    public TeamRepository(IMongoDatabase database) : base(database, "Teams")
    {
        _collection = database.GetCollection<Team>("Teams");
    }
 
    public async Task<IEnumerable<Team>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return [];
 
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Regex(t => t.Name, new BsonRegularExpression(name, "i")),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
 
    public async Task<bool> ExistsByNameAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
 
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Eq(t => t.Name, name),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        if (!string.IsNullOrWhiteSpace(excludeId))
            filter = Builders<Team>.Filter.And(filter, Builders<Team>.Filter.Ne(t => t.Id, excludeId));
 
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
 
    public async Task<PagedList<Team>> GetByStatusAsync(TeamStatus status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Eq(t => t.Status, status),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await PagedList<Team>.ToPagedListAsync(
            _collection.Find(filter).Sort(Builders<Team>.Sort.Descending(t => t.CreatedAt)),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<PagedList<Team>> GetByCategoryAsync(string category, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Eq(t => t.Category, category),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await PagedList<Team>.ToPagedListAsync(
            _collection.Find(filter).Sort(Builders<Team>.Sort.Descending(t => t.CreatedAt)),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<IEnumerable<Team>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag)) return [];
 
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.AnyEq(t => t.Tags, tag),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<Team>> GetByMemberIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];
 
        // UserSnapshot вкладений — звертаємось до members[].user.userId
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Eq("members.user.userId", userId),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
 
    public async Task<bool> IsUserMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(userId)) return false;
 
        var filter = Builders<Team>.Filter.And(
            Builders<Team>.Filter.Eq(t => t.Id, teamId),
            Builders<Team>.Filter.Eq("members.user.userId", userId),
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false));
 
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
 
    public async Task<PagedList<Team>> GetPagedFilteredAsync(
        string? name, string? category, string? tag, TeamStatus? status,
        string? memberId, string? requiredRole,
        int pageNumber, int pageSize,
        string? orderBy = null, bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Team>>
        {
            Builders<Team>.Filter.Eq(t => t.IsDeleted, false)
        };
 
        if (!string.IsNullOrWhiteSpace(name))
            filters.Add(Builders<Team>.Filter.Regex(t => t.Name, new BsonRegularExpression(name, "i")));
 
        if (!string.IsNullOrWhiteSpace(category))
            filters.Add(Builders<Team>.Filter.Eq(t => t.Category, category));
 
        if (!string.IsNullOrWhiteSpace(tag))
            filters.Add(Builders<Team>.Filter.AnyEq(t => t.Tags, tag));
 
        if (status.HasValue)
            filters.Add(Builders<Team>.Filter.Eq(t => t.Status, status.Value));
 
        // Оновлено: UserSnapshot вкладений
        if (!string.IsNullOrWhiteSpace(memberId))
            filters.Add(Builders<Team>.Filter.Eq("members.user.userId", memberId));
 
        if (!string.IsNullOrWhiteSpace(requiredRole))
            filters.Add(Builders<Team>.Filter.ElemMatch(t => t.RequiredRoles, r => r.Role == requiredRole));
 
        var combinedFilter = Builders<Team>.Filter.And(filters);
 
        var sort = orderBy?.ToLower() switch
        {
            "name" => ascending ? Builders<Team>.Sort.Ascending(t => t.Name) : Builders<Team>.Sort.Descending(t => t.Name),
            _ => ascending ? Builders<Team>.Sort.Ascending(t => t.CreatedAt) : Builders<Team>.Sort.Descending(t => t.CreatedAt)
        };
 
        return await PagedList<Team>.ToPagedListAsync(
            _collection.Find(combinedFilter).Sort(sort),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<PagedList<Team>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Team>.Filter.Eq(t => t.IsDeleted, false);
 
        if (!string.IsNullOrWhiteSpace(cursor))
            filter = Builders<Team>.Filter.And(filter, Builders<Team>.Filter.Gt(t => t.Id, cursor));
 
        var items = await _collection.Find(filter)
            .Sort(Builders<Team>.Sort.Ascending(t => t.Id))
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
 
        return PagedList<Team>.FromCursor(items, pageSize);
    }
 
    // Викликається при отриманні події UserUpdated з RabbitMQ
    // Оновлює денормалізований snapshot у всіх командах де цей юзер є членом
    public async Task UpdateMemberSnapshotAsync(string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
 
        var filter = Builders<Team>.Filter.Eq("members.user.userId", userId);
 
        var update = Builders<Team>.Update
            .Set("members.$[elem].user.username", username)
            .Set("members.$[elem].user.avatarUrl", avatarUrl)
            .Set("updatedAt", DateTime.UtcNow)
            .Set("updatedBy", userId);
 
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                new MongoDB.Bson.BsonDocument("elem.user.userId", userId))
        };
 
        await _collection.UpdateManyAsync(
            filter, update,
            new UpdateOptions { ArrayFilters = arrayFilters },
            cancellationToken);
    }
}