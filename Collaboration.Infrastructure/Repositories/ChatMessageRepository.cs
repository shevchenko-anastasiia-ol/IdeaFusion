using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
{
    private readonly IMongoCollection<ChatMessage> _collection;

    public ChatMessageRepository(IMongoDatabase database) : base(database, "ChatMessages")
    {
        _collection = database.GetCollection<ChatMessage>("ChatMessages");
    }

    public async Task<PagedList<ChatMessage>> GetByChatPagedAsync(
        string chatId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatId)) return new PagedList<ChatMessage>([], 0, 1, pageSize);

        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.ChatId, chatId),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false));

        return await PagedList<ChatMessage>.ToPagedListAsync(
            _collection.Find(filter).Sort(Builders<ChatMessage>.Sort.Descending(m => m.CreatedAt)),
            pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedList<ChatMessage>> GetByChatCursorAsync(
        string chatId, string? beforeId, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatId)) return new PagedList<ChatMessage>([], 0, 1, pageSize);

        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.ChatId, chatId),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false));

        if (!string.IsNullOrWhiteSpace(beforeId))
        {
            var reference = await _collection.Find(
                Builders<ChatMessage>.Filter.Eq(m => m.Id, beforeId))
                .FirstOrDefaultAsync(cancellationToken);

            if (reference is not null)
                filter = Builders<ChatMessage>.Filter.And(filter,
                    Builders<ChatMessage>.Filter.Lt(m => m.CreatedAt, reference.CreatedAt));
        }

        var items = await _collection.Find(filter)
            .Sort(Builders<ChatMessage>.Sort.Descending(m => m.CreatedAt))
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        items.Reverse();

        return PagedList<ChatMessage>.FromCursor(items, pageSize);
    }

    public async Task<ChatMessage?> GetLatestByChatIdAsync(string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatId)) return null;

        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.ChatId, chatId),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false));

        return await _collection.Find(filter)
            .Sort(Builders<ChatMessage>.Sort.Descending(m => m.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetLatestPerChatAsync(
        IEnumerable<string> chatIds, CancellationToken cancellationToken = default)
    {
        var ids = chatIds.ToList();
        if (ids.Count == 0) return [];

        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.In(m => m.ChatId, ids),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false));

        var all = await _collection.Find(filter)
            .Sort(Builders<ChatMessage>.Sort.Descending(m => m.CreatedAt))
            .ToListAsync(cancellationToken);

        // Найновіше повідомлення на кожен чат
        var latest = new List<ChatMessage>(all.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in all)
        {
            if (seen.Add(message.ChatId))
                latest.Add(message);
        }

        return latest;
    }

    // Викликається при отриманні події UserUpdated — оновлює snapshot відправника у всіх повідомленнях
    public async Task UpdateSenderSnapshotAsync(
        string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var filter = Builders<ChatMessage>.Filter.Eq("sender.userId", userId);

        var update = Builders<ChatMessage>.Update
            .Set("sender.username", username)
            .Set("sender.avatarUrl", avatarUrl)
            .Set("updatedAt", DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}