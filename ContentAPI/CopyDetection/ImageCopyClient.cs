using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ContentAPI.CopyDetection;

public interface IImageCopyClient
{
    Task<CopyCheckResult> CheckFileAsync(Stream image, string fileName, string contentType,
        IEnumerable<string> excludeUsers, IEnumerable<string> excludeTeams, CancellationToken ct = default);

    Task<CopyCheckResult> CheckObjectAsync(string bucket, string objectName,
        IEnumerable<string> excludeUsers, IEnumerable<string> excludeTeams, CancellationToken ct = default);

    Task IndexAsync(CopyIndexRequest request, CancellationToken ct = default);
    Task<int> DeletePostAsync(int postId, CancellationToken ct = default);
    Task<int> ClearIndexAsync(CancellationToken ct = default);
    Task<int> RecordCheckAsync(CopyCheckRecordRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CopyCheckRecord>> ListChecksAsync(IEnumerable<string> zones, string? status, int limit, int offset, CancellationToken ct = default);
    Task<CopyCheckRecord?> GetCheckAsync(int id, CancellationToken ct = default);
    Task<CopyCheckRecord?> DecideAsync(int id, CopyCheckDecision decision, CancellationToken ct = default);
    Task<byte[]?> GetExplanationAsync(int id, CancellationToken ct = default);
}

/// <summary>Typed HTTP client of the Python copy detection service (ml/service).</summary>
public sealed class ImageCopyClient : IImageCopyClient
{
    private readonly HttpClient _http;

    public ImageCopyClient(HttpClient http) => _http = http;

    public async Task<CopyCheckResult> CheckFileAsync(Stream image, string fileName, string contentType,
        IEnumerable<string> excludeUsers, IEnumerable<string> excludeTeams, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(image);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var type)
            ? type
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", string.IsNullOrWhiteSpace(fileName) ? "image" : fileName);
        AddExclusions(form, excludeUsers, excludeTeams);
        return await PostCheckAsync(form, ct);
    }

    public async Task<CopyCheckResult> CheckObjectAsync(string bucket, string objectName,
        IEnumerable<string> excludeUsers, IEnumerable<string> excludeTeams, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(bucket), "bucket" },
            { new StringContent(objectName), "object_name" },
        };
        AddExclusions(form, excludeUsers, excludeTeams);
        return await PostCheckAsync(form, ct);
    }

    public async Task IndexAsync(CopyIndexRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("v1/index", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> DeletePostAsync(int postId, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"v1/index/posts/{postId}", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(cancellationToken: ct);
        return body?.GetValueOrDefault("deleted") ?? 0;
    }

    public async Task<int> ClearIndexAsync(CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync("v1/index", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(cancellationToken: ct);
        return body?.GetValueOrDefault("deleted") ?? 0;
    }

    public async Task<int> RecordCheckAsync(CopyCheckRecordRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("v1/checks", request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(cancellationToken: ct);
        return body?.GetValueOrDefault("id") ?? 0;
    }

    public async Task<IReadOnlyList<CopyCheckRecord>> ListChecksAsync(IEnumerable<string> zones, string? status,
        int limit, int offset, CancellationToken ct = default)
    {
        var query = string.Join("&", zones.Select(z => $"zone={Uri.EscapeDataString(z)}"));
        query += $"&status={Uri.EscapeDataString(status ?? "")}&limit={limit}&offset={offset}";
        return await _http.GetFromJsonAsync<List<CopyCheckRecord>>($"v1/checks?{query.TrimStart('&')}", ct) ?? new();
    }

    public async Task<CopyCheckRecord?> GetCheckAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"v1/checks/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopyCheckRecord>(cancellationToken: ct);
    }

    public async Task<CopyCheckRecord?> DecideAsync(int id, CopyCheckDecision decision, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"v1/checks/{id}/decision", decision, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopyCheckRecord>(cancellationToken: ct);
    }

    public async Task<byte[]?> GetExplanationAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"v1/checks/{id}/explanation.png", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<CopyCheckResult> PostCheckAsync(MultipartFormDataContent form, CancellationToken ct)
    {
        using var response = await _http.PostAsync("v1/check", form, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopyCheckResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Copy detection service returned an empty body.");
    }

    private static void AddExclusions(MultipartFormDataContent form, IEnumerable<string> users, IEnumerable<string> teams)
    {
        form.Add(new StringContent(string.Join(",", users.Where(u => !string.IsNullOrWhiteSpace(u)))), "exclude_user_ids");
        form.Add(new StringContent(string.Join(",", teams.Where(t => !string.IsNullOrWhiteSpace(t)))), "exclude_team_ids");
    }
}
