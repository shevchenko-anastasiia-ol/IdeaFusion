using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class DirectConversationRepository : GenericRepository<DirectConversation>, IDirectConversationRepository
{
    private readonly IMongoCollection<DirectConversation> _collection;

    public DirectConversationRepository(IMongoDatabase database) : base(database, "DirectConversations")
    {
        _collection = database.GetCollection<DirectConversation>("DirectConversations");
    }

    public async Task<DirectConversation?> GetByPairAsync(string userA, string userB, CancellationToken cancellationToken = default)
    {
        var key = DirectConversation.KeyFor(userA, userB);
        return await _collection
            .Find(c => c.PairKey == key && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DirectConversation>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<DirectConversation>.Filter.And(
            Builders<DirectConversation>.Filter.AnyEq(c => c.ParticipantIds, userId),
            Builders<DirectConversation>.Filter.Eq(c => c.IsDeleted, false));

        var list = await _collection.Find(filter).ToListAsync(cancellationToken);
        return list
            .OrderByDescending(c => c.LastMessageAt ?? c.UpdatedAt ?? c.CreatedAt)
            .ToList();
    }
}
