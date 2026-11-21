using System.Net;
using System.Text.Json;
using FluentAssertions;
using IdentityAPI.Keycloak;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.Tests.Keycloak;

public class KeycloakAdminClientTests : IDisposable
{
    private const string BaseAddress = "https://keycloak.test/admin/realms/IdeaFusion/";
    private const string TokenUrl    = "https://keycloak.test/realms/IdeaFusion/protocol/openid-connect/token";

    private readonly HttpMessageHandlerStub _http = new();
    private readonly KeycloakAdminClient _client;

    public KeycloakAdminClientTests()
    {
        Environment.SetEnvironmentVariable("Keycloak__Url", "https://keycloak.test");
        Environment.SetEnvironmentVariable("Keycloak__Realm", "IdeaFusion");

        var factory = new StubHttpClientFactory(_http, BaseAddress);
        _client = new KeycloakAdminClient(
            factory,
            Options.Create(new KeycloakAdminOptions
            {
                ClientId = "ideafusion-admin",
                ClientSecret = "test-secret"
            }));
    }

    public void Dispose()
    {
        _client.Dispose();
        _http.Dispose();
        Environment.SetEnvironmentVariable("Keycloak__Url", null);
        Environment.SetEnvironmentVariable("Keycloak__Realm", null);
    }

    // ── Token acquisition ─────────────────────────────────────────────────────

    [Fact]
    public async Task Token_IsRequestedOnceAndReusedAcrossCalls()
    {
        _http.EnqueueToken("token-1");
        _http.EnqueueJson("[]");

        await _client.GetUserRolesAsync(Guid.NewGuid());
        await _client.GetUserRolesAsync(Guid.NewGuid());

        _http.Requests.Count(r => r.RequestUri!.ToString() == TokenUrl).Should().Be(1);
    }

    [Fact]
    public async Task Token_UsesTheClientCredentialsGrant()
    {
        _http.EnqueueToken("token-1");
        _http.EnqueueJson("[]");

        await _client.GetUserRolesAsync(Guid.NewGuid());

        var body = _http.BodyFor(TokenUrl);

        body.Should().Contain("grant_type=client_credentials");
        body.Should().Contain("client_id=ideafusion-admin");
        body.Should().Contain("client_secret=test-secret");

        // A password grant would put a user password on the wire.
        body.Should().NotContain("password=");
    }

    [Fact]
    public async Task Token_IsSentAsABearerHeader()
    {
        _http.EnqueueToken("token-1");
        _http.EnqueueJson("[]");

        await _client.GetUserRolesAsync(Guid.NewGuid());

        var call = _http.Requests.First(r => r.RequestUri!.ToString() != TokenUrl);
        call.Headers.Authorization!.Scheme.Should().Be("Bearer");
        call.Headers.Authorization!.Parameter.Should().Be("token-1");
    }

    [Fact]
    public async Task Token_IsRefreshedOnceItIsWithinTheSkewWindow()
    {
        // A 30s lifetime against a 60s default skew must never be cached, or the
        // client would present an already-dead token.
        _http.EnqueueToken("token-1", expiresIn: 30);
        _http.EnqueueJson("[]");
        _http.EnqueueToken("token-2", expiresIn: 30);
        _http.EnqueueJson("[]");

        await _client.GetUserRolesAsync(Guid.NewGuid());
        await _client.GetUserRolesAsync(Guid.NewGuid());

        _http.Requests.Count(r => r.RequestUri!.ToString() == TokenUrl).Should().Be(2);
    }

    [Fact]
    public async Task Token_AFailureIsReportedAsAKeycloakAdminException()
    {
        _http.EnqueueStatus(HttpStatusCode.Unauthorized);

        var act = () => _client.GetUserRolesAsync(Guid.NewGuid());

        (await act.Should().ThrowAsync<KeycloakAdminException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_AMissingClientSecretIsReportedClearly()
    {
        var factory = new StubHttpClientFactory(_http, BaseAddress);
        using var client = new KeycloakAdminClient(
            factory,
            Options.Create(new KeycloakAdminOptions { ClientId = "ideafusion-admin", ClientSecret = "" }));

        var act = () => client.GetUserRolesAsync(Guid.NewGuid());

        (await act.Should().ThrowAsync<KeycloakAdminException>())
            .WithMessage("*client secret*");
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_PassesTheSearchTermThrough()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("[]");

        await _client.SearchUsersAsync("anna");

        var call = _http.Requests.Last();
        call.RequestUri!.Query.Should().Contain("search=anna");
        call.RequestUri!.Query.Should().Contain("exact=false");
    }

    [Fact]
    public async Task Search_OmitsTheSearchParameterWhenNoTermIsGiven()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("[]");

        await _client.SearchUsersAsync(null);

        _http.Requests.Last().RequestUri!.Query.Should().NotContain("search=");
    }

    [Fact]
    public async Task Search_EscapesTheSearchTerm()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("[]");

        await _client.SearchUsersAsync("a b&c");

        // Read the raw query: Uri.ToString() hands back a decoded space.
        _http.Requests.Last().RequestUri!.Query.Should().Contain("search=a%20b%26c");
    }

    [Fact]
    public async Task Search_ClampsThePageSize()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("[]");

        await _client.SearchUsersAsync(null, first: -5, max: 10_000);

        var query = _http.Requests.Last().RequestUri!.Query;
        query.Should().Contain("first=0");
        query.Should().Contain("max=100");
    }

    [Fact]
    public async Task Search_MapsTheRepresentation()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("""
        [
          {
            "id": "22222222-2222-2222-2222-222222222222",
            "username": "AnnoGray",
            "email": "anna@ideafusion.com",
            "emailVerified": true,
            "enabled": true,
            "realmRoles": ["author"],
            "requiredActions": [],
            "createdTimestamp": 1700000000000
          }
        ]
        """);
        _http.EnqueueJson("""[{ "id": "c1", "type": "password" }, { "id": "c2", "type": "otp" }]""");

        var users = await _client.SearchUsersAsync("anna");

        var user = users.Should().ContainSingle().Subject;
        user.Id.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        user.Username.Should().Be("AnnoGray");
        user.Email.Should().Be("anna@ideafusion.com");
        user.RealmRoles.Should().BeEquivalentTo(["author"]);
        user.TwoFactorConfigured.Should().BeTrue();
        user.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_SkipsRowsWithAnUnparseableId()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("""
        [
          { "id": "not-a-guid", "username": "broken" },
          { "id": "22222222-2222-2222-2222-222222222222", "username": "AnnoGray" }
        ]
        """);

        var users = await _client.SearchUsersAsync(null);

        users.Should().ContainSingle();
        users[0].Username.Should().Be("AnnoGray");
    }

    [Fact]
    public async Task Search_ReportsTwoFactorOffWithoutAnOtpCredential()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("""[{ "id": "22222222-2222-2222-2222-222222222222", "username": "AnnoGray" }]""");
        _http.EnqueueJson("""[{ "id": "c1", "type": "password" }]""");

        var users = await _client.SearchUsersAsync(null);

        users.Should().ContainSingle().Which.TwoFactorConfigured.Should().BeFalse();
        _http.Requests.Last().RequestUri!.ToString()
            .Should().EndWith("users/22222222-2222-2222-2222-222222222222/credentials");
    }

    // ── 2FA ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TwoFactor_IsConfiguredWhenAnOtpCredentialExists()
    {
        var userId = Guid.NewGuid();
        _http.EnqueueToken("t");
        _http.EnqueueJson("""[{ "id": "c1", "type": "password" }, { "id": "c2", "type": "otp" }]""");

        (await _client.IsOtpConfiguredAsync(userId)).Should().BeTrue();
        _http.Requests.Last().RequestUri!.ToString().Should().Be($"{BaseAddress}users/{userId}/credentials");
    }

    [Fact]
    public async Task TwoFactor_IsNotConfiguredWithOnlyAPassword()
    {
        // The seeded users: no pending CONFIGURE_TOTP action, and no authenticator either.
        _http.EnqueueToken("t");
        _http.EnqueueJson("""[{ "id": "c1", "type": "password" }]""");

        (await _client.IsOtpConfiguredAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task TwoFactor_IsNotConfiguredWithoutAnyCredential()
    {
        // Users who only sign in through Google have no credentials in Keycloak at all.
        _http.EnqueueToken("t");
        _http.EnqueueJson("[]");

        (await _client.IsOtpConfiguredAsync(Guid.NewGuid())).Should().BeFalse();
    }

    // ── Role changes ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GrantRoles_ResolvesTheRoleIdBeforePosting()
    {
        var id = Guid.NewGuid();

        _http.EnqueueToken("t");
        _http.EnqueueJson("""{ "id": "role-moderator-id", "name": "moderator" }""");   // GET roles/moderator
        _http.EnqueueStatus(HttpStatusCode.NoContent);                                    // POST role-mappings
        _http.EnqueueJson("""[{ "id": "role-moderator-id", "name": "moderator" }]""");   // GET composite

        await _client.AddRolesAsync(id, ["moderator"]);

        var body = _http.BodyFor("users/" + id + "/role-mappings/realm", HttpMethod.Post);
        body.Should().Contain("role-moderator-id");
        body.Should().Contain("moderator");
    }

    [Fact]
    public async Task GrantRoles_CachesTheResolvedRoleId()
    {
        var id = Guid.NewGuid();

        _http.EnqueueToken("t");
        _http.EnqueueJson("""{ "id": "role-author-id", "name": "author" }""");
        _http.EnqueueStatus(HttpStatusCode.NoContent);
        _http.EnqueueJson("""[]""");
        _http.EnqueueStatus(HttpStatusCode.NoContent);
        _http.EnqueueJson("""[]""");

        await _client.AddRolesAsync(id, ["author"]);
        await _client.AddRolesAsync(id, ["author"]);

        _http.Requests.Count(r => r.RequestUri!.ToString() == $"{BaseAddress}roles/author").Should().Be(1);
    }

    [Fact]
    public async Task RevokeRoles_UsesADeleteAgainstTheSameEndpoint()
    {
        var id = Guid.NewGuid();

        _http.EnqueueToken("t");
        _http.EnqueueJson("""{ "id": "role-author-id", "name": "author" }""");
        _http.EnqueueStatus(HttpStatusCode.NoContent);
        _http.EnqueueJson("""[]""");

        await _client.RemoveRolesAsync(id, ["author"]);

        _http.Requests.Should().Contain(r => r.Method == HttpMethod.Delete
            && r.RequestUri!.ToString().EndsWith($"users/{id}/role-mappings/realm"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("superuser")]
    [InlineData("realm-admin")]
    public async Task GrantRoles_IgnoresRolesTheRealmDoesNotDefine(string role)
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("""[]""");   // only the follow-up role read

        var roles = await _client.AddRolesAsync(Guid.NewGuid(), [role]);

        // Nothing was sent, so nothing could have been granted.
        _http.Requests.Should().NotContain(r => r.Method == HttpMethod.Post
            && r.RequestUri!.ToString().Contains("role-mappings"));
        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task GrantRoles_DropsUnknownRolesButStillAppliesTheRest()
    {
        var id = Guid.NewGuid();

        _http.EnqueueToken("t");
        _http.EnqueueJson("""{ "id": "role-author-id", "name": "author" }""");
        _http.EnqueueStatus(HttpStatusCode.NoContent);
        _http.EnqueueJson("""[{ "name": "author" }]""");

        await _client.AddRolesAsync(id, ["author", "superuser"]);

        var body = _http.BodyFor("users/" + id + "/role-mappings/realm", HttpMethod.Post);

        body.Should().Contain("author");
        body.Should().NotContain("superuser");
    }

    [Fact]
    public async Task GrantRoles_NormalisesCaseAndDuplicates()
    {
        var id = Guid.NewGuid();

        _http.EnqueueToken("t");
        _http.EnqueueJson("""{ "id": "role-author-id", "name": "author" }""");
        _http.EnqueueStatus(HttpStatusCode.NoContent);
        _http.EnqueueJson("""[{ "name": "author" }]""");

        await _client.AddRolesAsync(id, ["Author", "author", "AUTHOR"]);

        var body = _http.BodyFor("users/" + id + "/role-mappings/realm", HttpMethod.Post);

        // One role object, not three.
        body.Split("role-author-id").Length.Should().Be(2);
    }

    [Fact]
    public async Task GetUserRoles_ReturnsTheCompositeRealmRoles()
    {
        _http.EnqueueToken("t");
        _http.EnqueueJson("""
        [{ "name": "author" }, { "name": "moderator" }, { "name": "" }]
        """);

        var roles = await _client.GetUserRolesAsync(Guid.NewGuid());

        roles.Should().BeEquivalentTo(["author", "moderator"]);
    }

    // ── Account console URL ───────────────────────────────────────────────────

    [Fact]
    public void AccountConsoleUrl_PointsAtTheRealmAccountConsole()
    {
        _client.AccountConsoleUrl.Should().Be("https://keycloak.test/realms/IdeaFusion/account/");
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        private readonly string _baseAddress;

        public StubHttpClientFactory(HttpMessageHandler handler, string baseAddress)
        {
            _handler = handler;
            _baseAddress = baseAddress;
        }

        public HttpClient CreateClient(string name) =>
            new(_handler, disposeHandler: false) { BaseAddress = new Uri(_baseAddress) };
    }

    private sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();
        private readonly List<(HttpMethod? Method, string Url, string? Body)> _calls = [];

        public List<HttpRequestMessage> Requests { get; } = [];

        /// <summary>
        /// The body captured for a call. Content has to be read here, inside
        /// SendAsync: the caller disposes the request and its content as soon as
        /// the call returns.
        /// </summary>
        public string? BodyFor(string urlFragment, HttpMethod? method = null)
            => _calls.FirstOrDefault(c =>
                    (method is null || c.Method == method) &&
                    c.Url.Contains(urlFragment, StringComparison.OrdinalIgnoreCase))
                .Body;

        public void EnqueueJson(string json) =>
            _responses.Enqueue(() => JsonResponse(json, HttpStatusCode.OK));

        public void EnqueueStatus(HttpStatusCode status) =>
            _responses.Enqueue(() => new HttpResponseMessage(status));

        public void EnqueueToken(string accessToken, int expiresIn = 300) =>
            _responses.Enqueue(() => JsonResponse(
                JsonSerializer.Serialize(new { access_token = accessToken, expires_in = expiresIn, token_type = "Bearer" }),
                HttpStatusCode.OK));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);

            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(CancellationToken.None);

            _calls.Add((request.Method, request.RequestUri!.ToString(), body));

            if (_responses.Count == 0)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };

            return _responses.Dequeue()();
        }

        private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status) =>
            new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
    }
}
