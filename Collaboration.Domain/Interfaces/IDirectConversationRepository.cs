using Collaboration.Domain.Common;
using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Interfaces;

public interface IDirectConversationRepository : IGenericRepository<DirectConversation>
{
    Task<DirectConversation?> GetByPairAsync(string userA, string userB, CancellationToken cancellationToken = default);

    /// <summary>Every conversation the user is part of, the most recently active first.</summary>
    Task<List<DirectConversation>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}
