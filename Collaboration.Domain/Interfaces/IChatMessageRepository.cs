using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface IChatMessageRepository : IGenericRepository<ChatMessage>
{
    // Історія повідомлень чату (страниці з найновіших)
    Task<PagedList<ChatMessage>> GetByChatPagedAsync(
        string chatId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Історія повідомлень із курсором (повідомлення старіші за beforeId)
    Task<PagedList<ChatMessage>> GetByChatCursorAsync(
        string chatId, string? beforeId, int pageSize, CancellationToken cancellationToken = default);

    // Найновіше повідомлення чату (для прев'ю у списку)
    Task<ChatMessage?> GetLatestByChatIdAsync(string chatId, CancellationToken cancellationToken = default);

    // Найновіше повідомлення по кожному з чатів одним запитом
    Task<IEnumerable<ChatMessage>> GetLatestPerChatAsync(
        IEnumerable<string> chatIds, CancellationToken cancellationToken = default);

    // Синхронізація snapshot відправника через подію UserUpdated
    Task UpdateSenderSnapshotAsync(string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default);
}