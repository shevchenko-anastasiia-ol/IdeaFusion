using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface INotificationSettingsRepository
{
    /// <summary>Null when the person never changed anything (everything is then on).</summary>
    Task<NotificationSettings?> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveAsync(NotificationSettings settings, CancellationToken cancellationToken = default);
}
