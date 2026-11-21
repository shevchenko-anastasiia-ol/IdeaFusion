using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ServiceDefaults.Security;

namespace IdentityAPI.Keycloak;

public sealed class KeycloakAdminClient : IKeycloakAdminClient, IDisposable
{
    /// <summary>Name of the configured named HttpClient.</summary>
    public const string HttpClientName = "keycloak-admin";

    private const string OtpCredentialType = "otp";
    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakAdminOptions _options;
    private readonly string _tokenEndpoint;
    private readonly string _realmRoot;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _realmRoleIds = new(StringComparer.OrdinalIgnoreCase);

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    public KeycloakAdminClient(IHttpClientFactory httpClientFactory, IOptions<KeycloakAdminOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;

        var authority = ResolveKeycloakUrl();
        var realm = ResolveRealm();

        _realmRoot = $"{authority.TrimEnd('/')}/realms/{realm}";
        _tokenEndpoint = $"{_realmRoot}/protocol/openid-connect/token";
    }

    public string AccountConsoleUrl => $"{_realmRoot}/account/";

    // ── Reads ──────────────────────────────────────────────

    public async Task<IReadOnlyList<KeycloakUserSummary>> SearchUsersAsync(
        string? search,
        int first = 0,
        int max = 25,
        CancellationToken cancellationToken = default)
    {
        first = Math.Max(first, 0);
        max = Math.Clamp(max, 1, MaxPageSize);

        var url = $"users?first={first}&max={max}&briefRepresentation=false";
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Keycloak matches username, first name, last name and email against
            // this one parameter, which is what "search users" should mean.
            url += $"&search={Uri.EscapeDataString(search.Trim())}&exact=false";
        }

        var users = await GetAsync<List<UserRepresentation>>(url, cancellationToken);
        if (users.Count == 0)
            return [];

        var summaries = new List<KeycloakUserSummary>(users.Count);
        foreach (var user in users)
        {
            if (!Guid.TryParse(user.Id, out var id))
                continue;

            summaries.Add(new KeycloakUserSummary
            {
                Id = id,
                Username = user.Username,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                Enabled = user.Enabled,
                RealmRoles = user.RealmRoles ?? [],
                // One request per row; a search page is at most MaxPageSize users.
                TwoFactorConfigured = await HasOtpCredentialAsync(id, cancellationToken),
                CreatedAt = user.CreatedAt
            });
        }

        return summaries;
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roles = await GetAsync<List<RoleRepresentation>>(
            $"users/{userId}/role-mappings/realm/composite", cancellationToken);

        return roles.Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList()!;
    }

    public Task<bool> IsOtpConfiguredAsync(Guid userId, CancellationToken cancellationToken = default)
        => HasOtpCredentialAsync(userId, cancellationToken);

    /// <summary>
    /// An authenticator app is set up exactly when the user has an "otp" credential. A missing
    /// CONFIGURE_TOTP required action says nothing: users who were never asked to set one up
    /// have no pending action either.
    /// </summary>
    private async Task<bool> HasOtpCredentialAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credentials = await GetAsync<List<CredentialRepresentation>>($"users/{userId}/credentials", cancellationToken);
        return credentials.Any(c => string.Equals(c.Type, OtpCredentialType, StringComparison.OrdinalIgnoreCase));
    }

    // ── Writes ─────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> AddRolesAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await ChangeRolesAsync(userId, roles, add: true, cancellationToken);
        return await GetUserRolesAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> RemoveRolesAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await ChangeRolesAsync(userId, roles, add: false, cancellationToken);
        return await GetUserRolesAsync(userId, cancellationToken);
    }

    private async Task ChangeRolesAsync(
        Guid userId,
        IEnumerable<string> roles,
        bool add,
        CancellationToken cancellationToken)
    {
        var requested = NormaliseRoles(roles);

        if (requested.Count == 0)
            return;

        // Keycloak's role-mapping endpoint needs the role's internal id, not its
        // name, so resolve each one first. Resolved ids are cached because the
        // realm's role set changes almost never.
        var payload = new List<RoleRepresentation>(requested.Count);
        foreach (var role in requested)
            payload.Add(new RoleRepresentation { Id = await ResolveRoleIdAsync(role, cancellationToken), Name = role });

        var url = $"users/{userId}/role-mappings/realm";
        using var request = new HttpRequestMessage(
            add ? HttpMethod.Post : HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        await SendAsync(request, expected: HttpStatusCode.NoContent, cancellationToken);
    }

    /// <summary>
    /// Restricts a requested role set to the three roles the realm defines, so a
    /// typo or a crafted request cannot create or grant a role that does not exist.
    /// </summary>
    private static IReadOnlyList<string> NormaliseRoles(IEnumerable<string> roles)
        => roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Where(r => r.Equals(IdeaFusionRoles.Author, StringComparison.OrdinalIgnoreCase)
                     || r.Equals(IdeaFusionRoles.Moderator, StringComparison.OrdinalIgnoreCase)
                     || r.Equals(IdeaFusionRoles.Administrator, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(r => r.ToLowerInvariant())
            .ToList();

    private async Task<string> ResolveRoleIdAsync(string roleName, CancellationToken cancellationToken)
    {
        if (_realmRoleIds.TryGetValue(roleName, out var cached))
            return cached;

        var role = await GetAsync<RoleRepresentation>($"roles/{Uri.EscapeDataString(roleName)}", cancellationToken);
        if (string.IsNullOrWhiteSpace(role.Id))
            throw new KeycloakAdminException($"Keycloak returned no id for realm role '{roleName}'.");

        _realmRoleIds[roleName] = role.Id;
        return role.Id;
    }

    // ── Token handling ─────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check: another caller may have refreshed while we waited.
            if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
                return _accessToken;

            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new KeycloakAdminException(
                    $"No client secret configured for '{_options.ClientId}'. " +
                    $"Set {KeycloakAdminOptions.SectionName}:{nameof(KeycloakAdminOptions.ClientSecret)}.");
            }

            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
            });

            using var response = await client.PostAsync(_tokenEndpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakAdminException(
                    $"Could not obtain a Keycloak admin token: {(int)response.StatusCode} {response.StatusCode}.",
                    response.StatusCode);
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
                        ?? throw new KeycloakAdminException("Keycloak returned an empty token response.");

            var skew = TimeSpan.FromSeconds(_options.TokenRefreshSkewSeconds);
            var lifetime = TimeSpan.FromSeconds(Math.Max(token.ExpiresIn, 0));

            // A token whose whole lifetime is inside the skew window would be
            // treated as already expired, so never cache past half its life.
            if (lifetime <= skew)
                lifetime = TimeSpan.FromSeconds(lifetime.TotalSeconds / 2);

            _accessToken = token.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.Add(lifetime) - skew;

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // ── HTTP plumbing ──────────────────────────────────────

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        using var response = await SendAsync(request, expected: null, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new KeycloakAdminException($"Keycloak returned an empty body for '{relativeUrl}'.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpStatusCode? expected,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));

        var response = await client.SendAsync(request, cancellationToken);

        var ok = expected is null ? response.IsSuccessStatusCode : response.StatusCode == expected;
        if (ok)
            return response;

        var body = await SafeReadAsync(response, cancellationToken);
        throw new KeycloakAdminException(
            $"Keycloak admin call to '{request.RequestUri}' failed: {(int)response.StatusCode} {response.StatusCode}. {body}",
            response.StatusCode);
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return text.Length > 500 ? text[..500] : text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ResolveKeycloakUrl()
        => Environment.GetEnvironmentVariable("Keycloak__Url")
           ?? Environment.GetEnvironmentVariable("KEYCLOAK_URL")
           ?? "http://localhost:8080";

    private static string ResolveRealm()
        => Environment.GetEnvironmentVariable("Keycloak__Realm")
           ?? Environment.GetEnvironmentVariable("KEYCLOAK_REALM")
           ?? "IdeaFusion";

    public void Dispose() => _tokenLock.Dispose();

    // ── Wire models ────────────────────────────────────────

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class CredentialRepresentation
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private sealed class RoleRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class UserRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("realmRoles")]
        public string[]? RealmRoles { get; set; }

        [JsonPropertyName("createdTimestamp")]
        public long CreatedTimestamp { get; set; }

        [JsonIgnore]
        public DateTimeOffset? CreatedAt => CreatedTimestamp <= 0
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimestamp);
    }
}
