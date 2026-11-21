using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class NotificationSettingsRepository : INotificationSettingsRepository
{
    private readonly IMongoCollection<NotificationSettings> _collection;

    public NotificationSettingsRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<NotificationSettings>("NotificationSettings");
    }

    public async Task<NotificationSettings?> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        await _collection.Find(s => s.UserId == userId).FirstOrDefaultAsync(cancellationToken);

    /// <summary>One document per person: replaced in place, created on the first save.</summary>
    public Task SaveAsync(NotificationSettings settings, CancellationToken cancellationToken = default) =>
        _collection.ReplaceOneAsync(
            s => s.UserId == settings.UserId,
            settings,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
}
