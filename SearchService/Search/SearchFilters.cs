using System.Text.Json;
using System.Text.Json.Serialization;
using SearchService.Normalization;

namespace SearchService.Search;

/// <summary>
/// The filters of a search without the query and the paging, as stored in history and saved searches (jsonb).
/// </summary>
public sealed record SearchFilters
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string? Kind { get; init; }
    public string? Category { get; init; }
    public string[]? Tags { get; init; }
    public TagMatch TagMode { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? MinLikes { get; init; }
    public bool HasOpenVacancies { get; init; }
    public string? Role { get; init; }
    public string? Author { get; init; }
    public string? Team { get; init; }
    public SearchSort Sort { get; init; }

    public static SearchFilters Of(SearchRequest r) => new SearchFilters
    {
        Kind = Blank(r.Kind), Category = Blank(r.Category),
        Tags = r.Tags.Count == 0 ? null : r.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).ToArray(),
        TagMode = r.TagMode, From = r.From, To = r.To, MinLikes = r.MinLikes is > 0 ? r.MinLikes : null,
        HasOpenVacancies = r.HasOpenVacancies, Role = Blank(r.Role),
        Author = Blank(r.AuthorId), Team = Blank(r.TeamId), Sort = r.Sort,
    }.Normalized();

    public SearchRequest ToRequest(string? query) => new()
    {
        Query = query, Kind = Kind, Category = Category, Tags = Tags ?? [], TagMode = TagMode,
        From = From, To = To, MinLikes = MinLikes, HasOpenVacancies = HasOpenVacancies, Role = Role,
        AuthorId = Author, TeamId = Team, Sort = Sort,
    };

    /// <summary>True when nothing but the sort order is set.</summary>
    [JsonIgnore]
    public bool IsEmpty => this with { Sort = default } == new SearchFilters();

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static SearchFilters FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new SearchFilters() : JsonSerializer.Deserialize<SearchFilters>(json, Json) ?? new SearchFilters();

    /// <summary>Same for the same search however it was typed: query normalized, filters in a fixed form.</summary>
    public static string Fingerprint(string? query, SearchFilters filters) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(TextNormalizer.Normalize(query) + "\n" + filters.ToJson())))[..32];

    // Records compare arrays by reference, so empty tags become null and the rest are kept sorted.
    private SearchFilters Normalized() => this with
    {
        Tags = Tags is { Length: > 0 } ? Tags.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray() : null,
    };

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public bool Equals(SearchFilters? other) =>
        other is not null && ToJson() == other.ToJson();

    public override int GetHashCode() => ToJson().GetHashCode();
}
