using Collaboration.Domain.Exceptions;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.ValueOfObjects;

/// <summary>
/// A file sent in a team chat. The bytes live in object storage (uploaded through the
/// media endpoint first); the message only keeps the link and what the client needs
/// to show it: an image or a video inline, anything else as a download.
/// </summary>
public sealed class ChatAttachment
{
    public const int MaxPerMessage = 10;
    public const string Image = "image";
    public const string Video = "video";
    public const string Audio = "audio";
    public const string File = "file";

    [BsonElement("url")]
    public string Url { get; private set; } = string.Empty;

    [BsonElement("kind")]
    public string Kind { get; private set; } = File;

    [BsonElement("name")]
    [BsonIgnoreIfNull]
    public string? Name { get; private set; }

    [BsonElement("contentType")]
    [BsonIgnoreIfNull]
    public string? ContentType { get; private set; }

    [BsonElement("size")]
    public long Size { get; private set; }

    [BsonConstructor]
    private ChatAttachment() { }

    public ChatAttachment(string url, string? name, string? contentType, long size)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 1000
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Attachment URL must be an absolute http(s) link.");
        if (size < 0)
            throw new DomainException("Attachment size cannot be negative.");

        Url = uri.ToString();
        Name = string.IsNullOrWhiteSpace(name) ? null : Truncate(name.Trim(), 255);
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : Truncate(contentType.Trim().ToLowerInvariant(), 100);
        Size = size;
        Kind = KindOf(ContentType);
    }

    /// <summary>Decided here rather than trusted from the client, so a message can't claim a file is a video.</summary>
    public static string KindOf(string? contentType) => contentType switch
    {
        null => File,
        _ when contentType.StartsWith("image/", StringComparison.Ordinal) => Image,
        _ when contentType.StartsWith("video/", StringComparison.Ordinal) => Video,
        _ when contentType.StartsWith("audio/", StringComparison.Ordinal) => Audio,
        _ => File,
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
