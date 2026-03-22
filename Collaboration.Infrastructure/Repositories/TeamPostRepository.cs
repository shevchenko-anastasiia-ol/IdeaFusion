using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class TeamPostRepository : GenericRepository<TeamPost>, ITeamPostRepository
{
    private readonly IMongoCollection<TeamPost> _collection;
 
    public TeamPostRepository(IMongoDatabase database) : base(database, "TeamPosts")
    {
        _collection = database.GetCollection<TeamPost>("TeamPosts");
    }
 
    public async Task<TeamPost?> GetByPostIdAsync(string postId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId)) return null;
 
        var filter = Builders<TeamPost>.Filter.And(
            Builders<TeamPost>.Filter.Eq(p => p.PostId, postId),
            Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false));
 
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<TeamPost>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return [];
 
        var filter = Builders<TeamPost>.Filter.And(
            Builders<TeamPost>.Filter.Eq(p => p.TeamId, teamId),
            Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false));
 
        return await _collection.Find(filter)
            .SortByDescending(p => p.PublishedAt)
            .ToListAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<TeamPost>> GetByAuthorIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];
 
        var filter = Builders<TeamPost>.Filter.And(
            Builders<TeamPost>.Filter.Eq("author.userId", userId),
            Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false));
 
        return await _collection.Find(filter)
            .SortByDescending(p => p.PublishedAt)
            .ToListAsync(cancellationToken);
    }
 
    public async Task<bool> ExistsByPostIdAsync(string postId, string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId) || string.IsNullOrWhiteSpace(teamId)) return false;
 
        var filter = Builders<TeamPost>.Filter.And(
            Builders<TeamPost>.Filter.Eq(p => p.PostId, postId),
            Builders<TeamPost>.Filter.Eq(p => p.TeamId, teamId),
            Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false));
 
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
 
    public async Task<PagedList<TeamPost>> GetByTeamPagedAsync(string teamId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TeamPost>.Filter.And(
            Builders<TeamPost>.Filter.Eq(p => p.TeamId, teamId),
            Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false));
 
        return await PagedList<TeamPost>.ToPagedListAsync(
            _collection.Find(filter).Sort(Builders<TeamPost>.Sort.Descending(p => p.PublishedAt)),
            pageNumber, pageSize, cancellationToken);
    }
 
    public async Task<PagedList<TeamPost>> GetByCursorAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TeamPost>.Filter.Eq(p => p.IsDeleted, false);
 
        if (!string.IsNullOrWhiteSpace(cursor))
            filter = Builders<TeamPost>.Filter.And(filter, Builders<TeamPost>.Filter.Gt(p => p.Id, cursor));
 
        var items = await _collection.Find(filter)
            .Sort(Builders<TeamPost>.Sort.Ascending(p => p.Id))
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
 
        return PagedList<TeamPost>.FromCursor(items, pageSize);
    }
 
    // Викликається при отриманні події UserUpdated з RabbitMQ
    // Оновлює денормалізований snapshot у всіх постах цього автора одним запитом
    public async Task UpdateAuthorSnapshotAsync(string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
 
        var filter = Builders<TeamPost>.Filter.Eq("author.userId", userId);
 
        var update = Builders<TeamPost>.Update
            .Set("author.username", username)
            .Set("author.avatarUrl", avatarUrl)
            .Set("updatedAt", DateTime.UtcNow)
            .Set("updatedBy", userId);
 
        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}