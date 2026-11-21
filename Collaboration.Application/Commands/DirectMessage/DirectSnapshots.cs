using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;

namespace Collaboration.Application.Commands.DirectMessage;

/// <summary>
/// Fills in names that were stored as bare ids because Identity could not be reached when
/// the conversation started. Cheap when there is nothing to fix: no call is made at all.
/// </summary>
public static class DirectSnapshots
{
    /// <returns>True when a participant's name or avatar changed and the conversation should be saved.</returns>
    public static async Task<bool> ResolveAsync(
        DirectConversation conversation, IUserSnapshotService users, CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var id in conversation.UnresolvedParticipants().ToList())
        {
            var (name, avatar) = await users.GetUserSnapshotDataAsync(id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(name) && name != id)
                changed |= conversation.RefreshParticipant(new UserSnapshot(id, name, avatar));
        }
        return changed;
    }
}
