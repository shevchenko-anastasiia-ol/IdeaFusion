using System.Collections.Concurrent;

namespace Collaboration.API.Notifications;

/// <summary>
/// Who has the app open right now (a live chat connection). Somebody who is online sees a new
/// message in the app, so they are not emailed about it.
/// </summary>
public sealed class PresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    public void Connected(string userId) => _connections.AddOrUpdate(userId, 1, (_, n) => n + 1);

    public void Disconnected(string userId)
    {
        while (_connections.TryGetValue(userId, out var n))
        {
            if (n <= 1 ? _connections.TryRemove(new KeyValuePair<string, int>(userId, n)) : _connections.TryUpdate(userId, n - 1, n))
                return;
        }
    }

    public bool IsOnline(string userId) => _connections.ContainsKey(userId);
}

/// <summary>
/// At most one "new messages" letter per conversation and recipient in <see cref="Window"/>:
/// a lively chat should not turn into a lively inbox.
/// </summary>
public sealed class MessageEmailThrottle
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, DateTime> _last = new();

    public bool ShouldSend(string conversationId, string recipientId)
    {
        var key = $"{conversationId}|{recipientId}";
        var now = DateTime.UtcNow;
        var sent = false;
        _last.AddOrUpdate(key,
            _ => { sent = true; return now; },
            (_, last) =>
            {
                if (now - last < Window) return last;
                sent = true;
                return now;
            });
        return sent;
    }
}
