namespace SearchService.Indexing;

public static class DocumentKinds
{
    public const string Post = "post";
    public const string Team = "team";
    public const string Vacancy = "vacancy";
    public const string User = "user";

    public static readonly string[] All = [Post, Team, Vacancy, User];
}

/// <summary>One searchable object as read from its source service.</summary>
public sealed record SourceDocument
{
    public required string Kind { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? Body { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Category names the source stores itself (a team's category), mapped like tags.</summary>
    public IReadOnlyList<string> CategoryHints { get; init; } = [];

    public string? ImageUrl { get; init; }
    public string? OwnerId { get; init; }
    public string? TeamId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int Likes { get; init; }
    public int Saves { get; init; }
    public int Views { get; init; }
    public int OpenVacancies { get; init; }

    public string Id => $"{Kind}:{SourceId}";
}

/// <summary>Reads every visible object of one kind from its source database.</summary>
public interface ISearchSource
{
    string Kind { get; }

    /// <summary>
    /// The complete current set. The indexer removes documents that are not in it,
    /// so an implementation must throw rather than return a partial list.
    /// </summary>
    Task<IReadOnlyList<SourceDocument>> ReadAllAsync(CancellationToken ct);
}

internal static class TextParts
{
    /// <summary>Joins the non-empty parts with " · ", or null when there are none.</summary>
    public static string? Join(params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToArray();
        return present.Length == 0 ? null : string.Join(" · ", present);
    }

    /// <summary>
    /// A user name fit for the index. Accounts registered by email have the address as user name;
    /// email addresses must not become searchable, so only the part before "@" is kept.
    /// </summary>
    public static string? PublicName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        var at = userName.IndexOf('@');
        return at < 0 ? userName.Trim() : at == 0 ? null : userName[..at].Trim();
    }

    /// <summary>The non-empty parts as separate lines.</summary>
    public static string Lines(IEnumerable<string?> parts) =>
        string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
}
