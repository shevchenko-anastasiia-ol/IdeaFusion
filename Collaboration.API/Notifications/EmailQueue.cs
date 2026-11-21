using System.Threading.Channels;

namespace Collaboration.API.Notifications;

/// <summary>
/// Letters waiting to be sent. Commands only drop a letter here, so an action never waits for
/// SMTP, and a mail outage never fails one. Bounded: under a flood the oldest letters go first.
/// </summary>
public sealed class EmailQueue
{
    private readonly Channel<EmailNotification> _channel = Channel.CreateBounded<EmailNotification>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _recent = new();

    /// <summary>
    /// Queues a letter, unless the very same one (recipient, subject, text) was queued in the last
    /// minute: a double click or a retried request must not mean two identical letters.
    /// </summary>
    public void Enqueue(EmailNotification notification)
    {
        var now = DateTime.UtcNow;
        var key = $"{notification.RecipientUserId}|{notification.Subject}|{notification.Text}";
        if (_recent.TryGetValue(key, out var at) && now - at < TimeSpan.FromMinutes(1))
            return;
        _recent[key] = now;
        if (_recent.Count > 5000)
            foreach (var old in _recent.Where(e => now - e.Value > TimeSpan.FromMinutes(1)).Select(e => e.Key).ToList())
                _recent.TryRemove(old, out _);
        _channel.Writer.TryWrite(notification);
    }

    public IAsyncEnumerable<EmailNotification> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
