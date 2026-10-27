using Npgsql;
using SearchService.Normalization;

namespace SearchService.Indexing;

/// <summary>
/// What the index knows about tags: synonym groups (tag_synonyms) and categories (tag_categories).
/// Every tag is reduced to a canonical key: its <see cref="TextNormalizer.TagKey"/>, replaced by the
/// key of its group's canonical tag when it has synonyms ("ілюстрація" → "illustration").
/// </summary>
public sealed class TagCatalog
{
    public static readonly TagCatalog Empty = Build([], []);

    private readonly Dictionary<string, string> _canonicalByKey;
    private readonly Dictionary<string, string[]> _namesByCanonical;
    private readonly Dictionary<string, string> _labelByCanonical;
    private readonly Dictionary<string, string[]> _categoriesByCanonical;
    private readonly HashSet<string> _categoryNames;

    private TagCatalog(
        Dictionary<string, string> canonicalByKey,
        Dictionary<string, string[]> namesByCanonical,
        Dictionary<string, string> labelByCanonical,
        Dictionary<string, string[]> categoriesByCanonical,
        HashSet<string> categoryNames)
    {
        _canonicalByKey = canonicalByKey;
        _namesByCanonical = namesByCanonical;
        _labelByCanonical = labelByCanonical;
        _categoriesByCanonical = categoriesByCanonical;
        _categoryNames = categoryNames;
    }

    /// <param name="synonyms">(term, canonical term) rows of tag_synonyms.</param>
    /// <param name="categories">(tag, category) rows of tag_categories.</param>
    public static TagCatalog Build(IEnumerable<(string Term, string Canonical)> synonyms, IEnumerable<(string Tag, string Category)> categories)
    {
        var canonicalByKey = new Dictionary<string, string>();
        var names = new Dictionary<string, List<string>>();
        var labels = new Dictionary<string, string>();
        foreach (var (term, canonical) in synonyms)
        {
            var canonicalKey = TextNormalizer.TagKey(canonical);
            var key = TextNormalizer.TagKey(term);
            if (key.Length == 0 || canonicalKey.Length == 0) continue;

            canonicalByKey[key] = canonicalKey;
            canonicalByKey[canonicalKey] = canonicalKey;
            labels.TryAdd(canonicalKey, canonical.Trim());
            if (!names.TryGetValue(canonicalKey, out var list)) names[canonicalKey] = list = [canonical.Trim()];
            if (!list.Contains(term.Trim())) list.Add(term.Trim());
        }

        string Canon(string key) => canonicalByKey.GetValueOrDefault(key, key);

        var categoriesByCanonical = new Dictionary<string, SortedSet<string>>();
        var categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (tag, category) in categories)
        {
            categoryNames.Add(category);
            var key = Canon(TextNormalizer.TagKey(tag));
            if (key.Length == 0) continue;
            if (!categoriesByCanonical.TryGetValue(key, out var set)) categoriesByCanonical[key] = set = new(StringComparer.Ordinal);
            set.Add(category);
        }

        return new TagCatalog(
            canonicalByKey,
            names.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()),
            labels,
            categoriesByCanonical.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()),
            categoryNames);
    }

    public static async Task<TagCatalog> LoadAsync(NpgsqlDataSource index, CancellationToken ct)
    {
        var synonyms = new List<(string, string)>();
        await using (var cmd = index.CreateCommand("SELECT term, canonical FROM tag_synonyms"))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) synonyms.Add((reader.GetString(0), reader.GetString(1)));

        var categories = new List<(string, string)>();
        await using (var cmd = index.CreateCommand("SELECT tag, category FROM tag_categories"))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) categories.Add((reader.GetString(0), reader.GetString(1)));

        return Build(synonyms, categories);
    }

    /// <summary>Canonical key of a tag as written, or "" for a tag without letters or digits.</summary>
    public string Key(string? tag)
    {
        var key = TextNormalizer.TagKey(tag);
        return key.Length == 0 ? key : _canonicalByKey.GetValueOrDefault(key, key);
    }

    /// <summary>All names of the tag's synonym group ("illustration", "ілюстрація", …), empty without synonyms.</summary>
    public IReadOnlyList<string> Synonyms(string canonicalKey) => _namesByCanonical.GetValueOrDefault(canonicalKey) ?? [];

    /// <summary>The display name of a synonym group, or null for a tag without synonyms.</summary>
    public string? Label(string canonicalKey) => _labelByCanonical.GetValueOrDefault(canonicalKey);

    /// <summary>Keys of every name that leads to this canonical key: its synonyms' keys and the key itself.</summary>
    public IEnumerable<string> NameKeys(string canonicalKey) =>
        Synonyms(canonicalKey).Select(TextNormalizer.TagKey).Append(canonicalKey).Distinct();

    /// <summary>Categories of a tag (by canonical key); a team's own category may also be a category name.</summary>
    public IEnumerable<string> Categories(string tagOrCategory)
    {
        if (_categoriesByCanonical.TryGetValue(Key(tagOrCategory), out var mapped)) return mapped;
        return _categoryNames.TryGetValue(tagOrCategory.Trim(), out var name) ? [name] : [];
    }
}

/// <summary>The current catalog for queries; the sync service refreshes it every cycle.</summary>
public sealed class TagCatalogCache(NpgsqlDataSource index)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(2);
    private (TagCatalog Catalog, DateTime LoadedAt)? _current;

    public void Set(TagCatalog catalog) => _current = (catalog, DateTime.UtcNow);

    public async Task<TagCatalog> GetAsync(CancellationToken ct)
    {
        var current = _current;
        if (current is { } c && DateTime.UtcNow - c.LoadedAt < MaxAge) return c.Catalog;
        var catalog = await TagCatalog.LoadAsync(index, ct);
        Set(catalog);
        return catalog;
    }
}
