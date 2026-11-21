using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IdentityBLL.DTOs;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Identity.Tests.Integration;

/// <summary>
/// End-to-end coverage of the profile endpoints against the real pipeline, with
/// Keycloak replaced by <see cref="TestAuthHandler"/> and the Keycloak admin API
/// replaced by a mock.
/// </summary>
public class AuthIntegrationTests : IClassFixture<IdentityWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminSubject    = "99999999-9999-9999-9999-999999999999";
    private const string ModeratorSubject = "44444444-4444-4444-4444-444444444444";
    private const string AuthorSubject   = "22222222-2222-2222-2222-222222222222";
    private const string TargetSubject   = "66666666-6666-6666-6666-666666666666";

    private readonly IdentityWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── /api/auth/me ───────────────────────────────────────

    [Fact]
    public async Task Me_CreatesTheProfileOnFirstCall()
    {
        var response = await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/me"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await ReadAsync<CurrentUserDto>(response);
        dto.Id.Should().Be(Guid.Parse(AuthorSubject));
        dto.JustProvisioned.Should().BeTrue();
        dto.Email.Should().Be("test@ideafusion.com");

        // Roles come from the token, never from a role table.
        dto.Roles.Should().BeEquivalentTo(["author"]);

        var stored = await GetUserAsync(Guid.Parse(AuthorSubject));
        stored.Should().NotBeNull();
        stored!.PasswordHash.Should().Be(ApplicationUser.ExternalPasswordSentinel);
    }

    [Fact]
    public async Task Me_ReusesTheExistingProfileOnLaterCalls()
    {
        var first = await ReadAsync<CurrentUserDto>(await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/me")));
        first.JustProvisioned.Should().BeTrue();

        var second = await ReadAsync<CurrentUserDto>(await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/me")));
        second.JustProvisioned.Should().BeFalse();
        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task Me_ReportsTheRolesPresentInTheToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add(TestAuthHandler.RolesHeader, "author,moderator,administrator");

        var dto = await ReadAsync<CurrentUserDto>(await _client.SendAsync(request));

        dto.Roles.Should().BeEquivalentTo(["author", "moderator", "administrator"]);
    }

    [Fact]
    public async Task Me_RejectsAnonymousCallers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add(TestAuthHandler.AnonymousHeader, "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /api/auth/me/2fa ───────────────────────────────────

    [Fact]
    public async Task TwoFactor_ReportsNotConfiguredAndLinksToTheAccountConsole()
    {
        var response = await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/me/2fa"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await ReadAsync<TwoFactorStatusDto>(response);
        dto.TwoFactorConfigured.Should().BeFalse();
        dto.AccountConsoleUrl.Should().Be("https://keycloak.test/realms/IdeaFusion/account/");
    }

    [Fact]
    public async Task TwoFactor_ReflectsAConfiguredOtp()
    {
        _factory.KeycloakAdminMock
            .Setup(c => c.IsOtpConfiguredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = await ReadAsync<TwoFactorStatusDto>(
            await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/me/2fa")));

        dto.TwoFactorConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task TwoFactor_RejectsAnonymousCallers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me/2fa");
        request.Headers.Add(TestAuthHandler.AnonymousHeader, "true");

        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /api/auth/users (Keycloak-backed search) ───────────

    [Fact]
    public async Task SearchUsers_RejectsAuthors()
    {
        var response = await _client.SendAsync(Authored(HttpMethod.Get, "/api/auth/users?search=anna"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchUsers_RejectsModerators()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users?search=anna");
        request.Headers.Add(TestAuthHandler.RolesHeader, "moderator");

        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchUsers_ReturnsMatchesForAdministrators()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users?search=anna");
        request.Headers.Add(TestAuthHandler.RolesHeader, "administrator");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await ReadAsync<List<KeycloakUserSummaryDto>>(response);
        users.Should().ContainSingle();
        users[0].Email.Should().Be("anna@ideafusion.com");
    }

    // ── Role changes ───────────────────────────────────────

    [Fact]
    public async Task GrantRoles_RejectsAuthors()
    {
        var response = await _client.SendAsync(
            Authored(HttpMethod.Post, $"/api/auth/users/{TargetSubject}/roles", new { roles = new[] { "moderator" } }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GrantRoles_AppliesTheChangeAndWritesAnAuditEntry()
    {
        var response = await _client.SendAsync(
            Admin(HttpMethod.Post, $"/api/auth/users/{TargetSubject}/roles", new { roles = new[] { "moderator" } }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadAsync<UserRolesResultDto>(response);
        result.UserId.Should().Be(Guid.Parse(TargetSubject));
        result.Roles.Should().Contain("moderator");

        var audit = await GetAuditAsync();
        var entry = audit.Should().ContainSingle(e => e.Action == "user.role.grant").Subject;
        entry.TargetId.Should().Be(TargetSubject);
        entry.ActorId.Should().Be(Guid.Parse(AdminSubject));
        entry.Category.Should().Be(AuditCategory.User);
    }

    [Fact]
    public async Task RevokeRoles_AppliesTheChangeAndWritesAnAuditEntry()
    {
        var response = await _client.SendAsync(
            Admin(HttpMethod.Delete, $"/api/auth/users/{TargetSubject}/roles", new { roles = new[] { "author" } }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var audit = await GetAuditAsync();
        audit.Should().ContainSingle(e => e.Action == "user.role.revoke");
    }

    [Fact]
    public async Task RevokeRoles_RefusesToStripYourOwnAdministratorRole()
    {
        var response = await _client.SendAsync(
            Admin(HttpMethod.Delete, $"/api/auth/users/{AdminSubject}/roles", new { roles = new[] { "administrator" } }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GrantRoles_RejectsAnEmptyRoleList()
    {
        var response = await _client.SendAsync(
            Admin(HttpMethod.Post, $"/api/auth/users/{TargetSubject}/roles", new { roles = Array.Empty<string>() }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GrantRoles_ReportsNotFoundWhenKeycloakHasNoSuchUser()
    {
        _factory.KeycloakAdminMock
            .Setup(c => c.AddRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdentityAPI.Keycloak.KeycloakAdminException(
                "no such user", System.Net.HttpStatusCode.NotFound));

        var response = await _client.SendAsync(
            Admin(HttpMethod.Post, $"/api/auth/users/{TargetSubject}/roles", new { roles = new[] { "author" } }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Retired endpoints ──────────────────────────────────

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/revoke")]
    [InlineData("/api/auth/logout")]
    [InlineData("/api/auth/change-password")]
    public async Task RetiredAuthEndpointsAreGone(string path)
    {
        var response = await _client.SendAsync(Authored(HttpMethod.Post, path, new { }));

        // The fallback policy rejects first, so the route itself is not even reached.
        // What matters is that none of these return a success status.
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // ── Helpers ────────────────────────────────────────────

    private static HttpRequestMessage Authored(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private static HttpRequestMessage Admin(HttpMethod method, string url, object? body)
    {
        var request = Authored(method, url, body);
        request.Headers.Add(TestAuthHandler.SubjectHeader, AdminSubject);
        request.Headers.Add(TestAuthHandler.RolesHeader, "administrator,author");
        return request;
    }

    private static Task<T> ReadAsync<T>(HttpResponseMessage response) => TestJson.ReadAsync<T>(response);

    private async Task<ApplicationUser?> GetUserAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>()
            .Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    private async Task<List<AuditLogEntry>> GetAuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>()
            .AuditLogEntries.AsNoTracking().ToListAsync();
    }
}
