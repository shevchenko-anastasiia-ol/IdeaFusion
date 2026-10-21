using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContentAPI.CopyDetection;

/// <summary>
/// Settings of the image copy detection integration (section "CopyDetection" in configuration).
/// </summary>
public sealed class CopyDetectionOptions
{
    public const string SectionName = "CopyDetection";

    /// <summary>Turns the integration off entirely; posts are then published without checks.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Base address of the ML service; resolved through Aspire service discovery.</summary>
    public string BaseUrl { get; set; } = "http://mlservice";

    /// <summary>A check runs SSCD, a vector search and SIFT on a few candidates: seconds on CPU.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public int QueueCapacity { get; set; } = 1000;
}

/// <summary>Risk zones and match types as the ML service reports them.</summary>
public static class CopyZones
{
    public const string Red = "red";
    public const string Yellow = "yellow";
    public const string Green = "green";
}

public sealed record CopyMatch
{
    [JsonPropertyName("media_id")] public int MediaId { get; init; }
    [JsonPropertyName("post_id")] public int PostId { get; init; }
    [JsonPropertyName("owner_user_id")] public string OwnerUserId { get; init; } = "";
    [JsonPropertyName("team_id")] public string? TeamId { get; init; }
    [JsonPropertyName("bucket")] public string Bucket { get; init; } = "";
    [JsonPropertyName("object_name")] public string ObjectName { get; init; } = "";
    [JsonPropertyName("p_copy")] public double PCopy { get; init; }
    [JsonPropertyName("zone")] public string Zone { get; init; } = CopyZones.Green;
    [JsonPropertyName("match_type")] public string MatchType { get; init; } = "none";
    [JsonPropertyName("signals")] public Dictionary<string, JsonElement> Signals { get; init; } = new();
    [JsonPropertyName("query_region")] public double[][]? QueryRegion { get; init; }
    [JsonPropertyName("reference_region")] public double[][]? ReferenceRegion { get; init; }
}

public sealed record CopyCheckResult
{
    [JsonPropertyName("zone")] public string Zone { get; init; } = CopyZones.Green;
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("match_type")] public string MatchType { get; init; } = "none";
    [JsonPropertyName("best")] public CopyMatch? Best { get; init; }
    [JsonPropertyName("matches")] public List<CopyMatch> Matches { get; init; } = new();
    [JsonPropertyName("model_version")] public string? ModelVersion { get; init; }
    [JsonPropertyName("index_size")] public int IndexSize { get; init; }
    [JsonPropertyName("timings_ms")] public Dictionary<string, double> TimingsMs { get; init; } = new();

    /// <summary>
    /// The result recomputed from the matches that are left after the platform's own
    /// authorship filter: the best remaining match decides the zone.
    /// </summary>
    public CopyCheckResult WithMatches(IReadOnlyList<CopyMatch> matches)
    {
        var best = matches.OrderByDescending(m => m.PCopy).FirstOrDefault();
        return this with
        {
            Matches = matches.ToList(),
            Best = best,
            Zone = best?.Zone ?? CopyZones.Green,
            Score = best?.PCopy ?? 0,
            MatchType = best?.MatchType ?? "none",
        };
    }
}

public sealed record CopyIndexRequest
{
    [JsonPropertyName("media_id")] public int MediaId { get; init; }
    [JsonPropertyName("post_id")] public int PostId { get; init; }
    [JsonPropertyName("owner_user_id")] public string OwnerUserId { get; init; } = "";
    [JsonPropertyName("team_id")] public string? TeamId { get; init; }
    [JsonPropertyName("bucket")] public string Bucket { get; init; } = "";
    [JsonPropertyName("object_name")] public string ObjectName { get; init; } = "";
}

public sealed record CopyCheckRecordRequest
{
    [JsonPropertyName("media_id")] public int? MediaId { get; init; }
    [JsonPropertyName("post_id")] public int? PostId { get; init; }
    [JsonPropertyName("owner_user_id")] public string? OwnerUserId { get; init; }
    [JsonPropertyName("team_id")] public string? TeamId { get; init; }
    [JsonPropertyName("bucket")] public string? Bucket { get; init; }
    [JsonPropertyName("object_name")] public string? ObjectName { get; init; }
    [JsonPropertyName("result")] public CopyCheckResult Result { get; init; } = new();
}

/// <summary>A stored check in the moderation queue.</summary>
public sealed record CopyCheckRecord
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("media_id")] public int? MediaId { get; init; }
    [JsonPropertyName("post_id")] public int? PostId { get; init; }
    [JsonPropertyName("owner_user_id")] public string? OwnerUserId { get; init; }
    [JsonPropertyName("team_id")] public string? TeamId { get; init; }
    [JsonPropertyName("zone")] public string Zone { get; init; } = CopyZones.Green;
    [JsonPropertyName("match_type")] public string MatchType { get; init; } = "none";
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("matched_media_id")] public int? MatchedMediaId { get; init; }
    [JsonPropertyName("matched_post_id")] public int? MatchedPostId { get; init; }
    [JsonPropertyName("result")] public CopyCheckResult Result { get; init; } = new();
    [JsonPropertyName("status")] public string Status { get; init; } = "open";
    [JsonPropertyName("decided_by")] public string? DecidedBy { get; init; }
    [JsonPropertyName("decided_at")] public DateTime? DecidedAt { get; init; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; init; }
}

public sealed record CopyCheckDecision
{
    /// <summary>"confirmed_copy", "dismissed" or "open".</summary>
    [JsonPropertyName("decision")] public string Decision { get; init; } = "";
    [JsonPropertyName("moderator")] public string Moderator { get; init; } = "";
}
