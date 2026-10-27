namespace SearchService.Search;

public sealed record SearchRequest
{
    public string? Query { get; init; }
    public string? Kind { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>All: documents with every tag (tag_keys @> …). Any: with at least one (tag_keys &amp;&amp; …).</summary>
    public TagMatch TagMode { get; init; } = TagMatch.Any;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record SearchHit
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? Snippet { get; init; }
    public string[] Tags { get; init; } = [];
    public string[] Categories { get; init; } = [];
    public string? ImageUrl { get; init; }
    public string? OwnerId { get; init; }
    public string? TeamId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int Likes { get; init; }
    public int Saves { get; init; }
    public int Views { get; init; }
    public int OpenVacancies { get; init; }
    public double Score { get; init; }
}

public sealed record SearchResponse
{
    public required string Query { get; init; }

    /// <summary>The query with misspelled words replaced, when that differs from what was typed ("did you mean").</summary>
    public string? DidYouMean { get; init; }

    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    /// <summary>Matches per kind for the same query and filters except the kind, for the result tabs.</summary>
    public IReadOnlyDictionary<string, int> KindCounts { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<SearchHit> Items { get; init; } = [];
}

public sealed record SuggestResponse
{
    /// <summary>The query with its last word completed from the index vocabulary.</summary>
    public IReadOnlyList<string> Completions { get; init; } = [];

    public IReadOnlyList<SearchHit> Documents { get; init; } = [];

    /// <summary>Tags matching the last word.</summary>
    public IReadOnlyList<TagSuggestion> Tags { get; init; } = [];
}

public sealed record CategoryCount(string Category, int Documents);

public enum TagMatch { Any, All }

/// <param name="Key">Canonical key: pass it (or any spelling) back as a tag filter.</param>
/// <param name="Label">Display name.</param>
/// <param name="Documents">Indexed documents with this tag.</param>
public sealed record TagSuggestion(string Key, string Label, int Documents);
