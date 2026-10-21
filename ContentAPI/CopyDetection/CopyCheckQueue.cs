using System.Threading.Channels;

namespace ContentAPI.CopyDetection;

public enum CopyJobKind
{
    /// <summary>Check the post's images against the index, record non-green results, then index them.</summary>
    IndexPost,

    /// <summary>Remove the post's images from the index (deleted or archived post).</summary>
    RemovePost,
}

public sealed record CopyJob(CopyJobKind Kind, int PostId);

/// <summary>
/// In-process queue between publishing and the ML service, so that a post is never slowed
/// down or blocked by the copy check. Bounded: when the ML service lags far behind, the oldest
/// jobs are dropped and logged rather than exhausting memory.
/// </summary>
public sealed class CopyCheckQueue
{
    private readonly Channel<CopyJob> _channel;

    public CopyCheckQueue(int capacity)
    {
        _channel = Channel.CreateBounded<CopyJob>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
    }

    public bool TryEnqueue(CopyJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<CopyJob> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
