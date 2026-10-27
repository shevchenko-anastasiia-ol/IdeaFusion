namespace SearchService.Search;

public sealed record SearchRequest
{
    public string? Query { get; init; }
    public string? Kind { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>All: documents with every tag (tag_keys @> …). Any: with at least one (tag_keys &amp;&amp; …).</summary>
    public TagMatch TagMode { get; init; } = TagMatch.Any;

    /// <summary>Created at or after (inclusive).</summary>
    public DateTime? From { get; init; }

    /// <summary>Created before (exclusive).</summary>
    public DateTime? To { get; init; }

    public int? MinLikes { get; init; }

    /// <summary>Teams only with open vacancies; other kinds are not affected.</summary>
    public bool HasOpenVacancies { get; init; }

    /// <summary>Vacancy role, specialization or a team's required role; matched fuzzily.</summary>
    public string? Role { get; init; }

    /// <summary>Author (Keycloak user id): their posts and their own profile.</summary>
    public string? AuthorId { get; init; }

    /// <summary>Team (collaboration id): the team, its vacancies and its posts.</summary>
    public string? TeamId { get; init; }

    public SearchSort Sort { get; init; } = SearchSort.Relevance;

    /// <summary>Continue after this cursor (from <see cref="SearchResponse.NextCursor"/>); <see cref="Page"/> is then ignored.</summary>
    public string? Cursor { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public enum SearchSort
{
    /// <summary>Best match first; without a query the same as <see cref="New"/>.</summary>
    Relevance,
    New,

    /// <summary>Reactions weighted by kind (view 1, like 3, save 4, open vacancy 2), halved every 14 days of age.</summary>
    Popular,
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

    /// <summary>The value the results are ordered by; goes into the cursor.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double SortKey { get; init; }
}

public sealed record FacetValue(string Value, int Count);

public sealed record TagFacet(string Key, string Label, int Count);

/// <summary>
/// Counts for the filter panel. Every facet ignores its own filter, so choosing "Арт" still shows
/// how many results "Музика" would give.
/// </summary>
public sealed record SearchFacets
{
    public IReadOnlyDictionary<string, int> Kinds { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<FacetValue> Categories { get; init; } = [];
    public IReadOnlyList<TagFacet> Tags { get; init; } = [];
}

public sealed record SearchResponse
{
    public required string Query { get; init; }

    /// <summary>The query with misspelled words replaced, when that differs from what was typed ("did you mean").</summary>
    public string? DidYouMean { get; init; }

    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public SearchSort Sort { get; init; }

    public SearchFacets Facets { get; init; } = new();

    public IReadOnlyList<SearchHit> Items { get; init; } = [];

    /// <summary>Pass as cursor= for the next page; null on the last page.</summary>
    public string? NextCursor { get; init; }
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
