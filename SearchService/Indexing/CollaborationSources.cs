using MongoDB.Bson;
using MongoDB.Driver;

namespace SearchService.Indexing;

/// <summary>
/// Teams and vacancies from the collaboration MongoDB, read as raw BSON so that the index does not
/// depend on the collaboration service's classes; the field names are its [BsonElement] names.
/// </summary>
internal static class Bson
{
    public static string? Str(BsonDocument d, string name) =>
        d.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;

    public static int Int(BsonDocument d, string name) =>
        d.TryGetValue(name, out var v) && v.IsNumeric ? v.ToInt32() : 0;

    public static DateTime Date(BsonDocument d, string name) =>
        d.TryGetValue(name, out var v) && v.IsValidDateTime ? v.ToUniversalTime() : d["_id"].AsObjectId.CreationTime;

    public static string[] Strings(BsonDocument d, string name) =>
        d.TryGetValue(name, out var v) && v.IsBsonArray
            ? v.AsBsonArray.Where(x => x.IsString).Select(x => x.AsString).ToArray()
            : [];

    /// <summary>Documents with isDeleted missing or false.</summary>
    public static readonly FilterDefinition<BsonDocument> NotDeleted =
        Builders<BsonDocument>.Filter.Ne("isDeleted", true);
}

public sealed class TeamSource(IMongoDatabase db) : ISearchSource
{
    public string Kind => DocumentKinds.Team;

    public async Task<IReadOnlyList<SourceDocument>> ReadAllAsync(CancellationToken ct)
    {
        var teams = await db.GetCollection<BsonDocument>("Teams").Find(Bson.NotDeleted).ToListAsync(ct);
        return teams.Select(t =>
        {
            // Open roles are what people search a team by ("шукаю команду, де потрібен аніматор").
            var roles = t.TryGetValue("requiredRoles", out var r) && r.IsBsonArray
                ? r.AsBsonArray.OfType<BsonDocument>()
                    .Select(x => TextParts.Join(Bson.Str(x, "role"), Bson.Str(x, "description")))
                : Enumerable.Empty<string?>();
            var category = Bson.Str(t, "category");
            return new SourceDocument
            {
                Kind = Kind,
                SourceId = t["_id"].ToString()!,
                Title = Bson.Str(t, "name") ?? "",
                Subtitle = TextParts.Join(category, Bson.Str(t, "status")),
                Body = TextParts.Lines(roles.Prepend(Bson.Str(t, "description"))),
                Tags = Bson.Strings(t, "tags"),
                Roles = RequiredRoles(t),
                CategoryHints = category is null ? [] : [category],
                ImageUrl = Bson.Str(t, "avatarUrl"),
                TeamId = t["_id"].ToString(),
                CreatedAt = Bson.Date(t, "createdAt"),
                OpenVacancies = Bson.Int(t, "openVacancyCount"),
            };
        }).Where(d => d.Title.Length > 0).ToList();
    }

    private static string[] RequiredRoles(BsonDocument team) =>
        team.TryGetValue("requiredRoles", out var r) && r.IsBsonArray
            ? r.AsBsonArray.OfType<BsonDocument>().Select(x => Bson.Str(x, "role")).OfType<string>()
                .Where(x => x.Length > 0).Distinct().ToArray()
            : [];
}

public sealed class VacancySource(IMongoDatabase db) : ISearchSource
{
    public string Kind => DocumentKinds.Vacancy;

    public async Task<IReadOnlyList<SourceDocument>> ReadAllAsync(CancellationToken ct)
    {
        var vacancies = await db.GetCollection<BsonDocument>("TeamVacancies").Find(Bson.NotDeleted).ToListAsync(ct);
        return vacancies.Where(IsOpen).Select(v =>
        {
            var category = Bson.Str(v, "category");
            return new SourceDocument
            {
                Kind = Kind,
                SourceId = v["_id"].ToString()!,
                Title = Bson.Str(v, "role") ?? "",
                Subtitle = TextParts.Join(Bson.Str(v, "teamName"), category),
                Body = TextParts.Lines([Bson.Str(v, "description"), Bson.Str(v, "requirements")]),
                Tags = Bson.Strings(v, "tags"),
                Roles = Bson.Str(v, "role") is { Length: > 0 } role ? [role] : [],
                CategoryHints = category is null ? [] : [category],
                ImageUrl = Bson.Str(v, "teamAvatarUrl"),
                TeamId = Bson.Str(v, "teamId"),
                CreatedAt = Bson.Date(v, "createdAt"),
            };
        }).Where(d => d.Title.Length > 0).ToList();
    }

    // TeamVacancyStatus has no custom serializer, so it is stored as a number (Open = 0);
    // a string is accepted too in case the representation changes.
    private static bool IsOpen(BsonDocument v) =>
        !v.TryGetValue("status", out var s)
        || (s.IsNumeric && s.ToInt32() == 0)
        || (s.IsString && s.AsString.Equals("Open", StringComparison.OrdinalIgnoreCase));
}
