using System.Text;
using System.Text.Json;

namespace SearchService.Search;

/// <summary>
/// Keyset position: the (sort key, created_at, id) of the last item shown. The next page starts
/// strictly after it, so documents indexed in between do not shift or repeat results the way an
/// offset would. <see cref="AsOf"/> is when the listing started.
/// </summary>
public sealed record SearchCursor(SearchSort Sort, double Key, DateTime CreatedAt, string Id, DateTime AsOf)
{
    private sealed record Payload(int S, double K, long C, string I, long A);

    public string Encode()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new Payload((int)Sort, Key, CreatedAt.Ticks, Id, AsOf.Ticks));
        return Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Null when the text is not a cursor this service issued.</summary>
    public static SearchCursor? TryDecode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 512) return null;
        try
        {
            var b64 = text.Replace('-', '+').Replace('_', '/');
            b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            var p = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
            if (p is null || p.I is null || !Enum.IsDefined(typeof(SearchSort), p.S)) return null;
            return new SearchCursor((SearchSort)p.S, p.K,
                new DateTime(p.C, DateTimeKind.Utc), p.I, new DateTime(p.A, DateTimeKind.Utc));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            return null;
        }
    }
}

public sealed class InvalidCursorException(string message) : ArgumentException(message);
