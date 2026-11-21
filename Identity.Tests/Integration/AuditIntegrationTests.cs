using System.Net;
using System.Text.Json;
using FluentAssertions;
using IdentityBLL.DTOs;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Identity.Tests.Integration;

/// <summary>
/// Covers the two audit read endpoints, including the boundary that matters most:
/// a moderator must never see anything outside the content slice.
/// </summary>
public class AuditIntegrationTests : IClassFixture<IdentityWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminSubject     = "99999999-9999-9999-9999-999999999999";
    private const string ModeratorSubject = "44444444-4444-4444-4444-444444444444";
    private const string AuthorSubject    = "22222222-2222-2222-2222-222222222222";

    private readonly IdentityWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditIntegrationTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/audit (administrators) ──────────────────────────────────────

    [Fact]
    public async Task FullTrail_RejectsAnonymousCallers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/audit");
        request.Headers.Add(TestAuthHandler.AnonymousHeader, "true");

        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullTrail_RejectsAuthors()
    {
        (await _client.SendAsync(As(HttpMethod.Get, "/api/audit", AuthorSubject, "author")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullTrail_RejectsModerators()
    {
        (await _client.SendAsync(As(HttpMethod.Get, "/api/audit", ModeratorSubject, "moderator")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullTrail_ReturnsEveryCategoryForAdministrators()
    {
        await SeedAsync("user.role.grant", AuditCategory.User);
        await SeedAsync("post.delete", AuditCategory.Content);
        await SeedAsync("login.failed", AuditCategory.Security);

        var response = await _client.SendAsync(As(HttpMethod.Get, "/api/audit", AdminSubject, "administrator"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = await ReadAsync<List<AuditLogEntryDto>>(response);
        entries.Select(e => e.Category).Should().BeEquivalentTo(["User", "Content", "Security"]);
    }

    // ── GET /api/audit/moderation (moderators) ───────────────────────────────

    [Fact]
    public async Task ModerationTrail_RejectsAuthors()
    {
        (await _client.SendAsync(As(HttpMethod.Get, "/api/audit/moderation", AuthorSubject, "author")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModerationTrail_ReturnsTheContentSliceForModerators()
    {
        await SeedAsync("user.role.grant", AuditCategory.User);
        await SeedAsync("login.failed", AuditCategory.Security);
        await SeedAsync("post.delete", AuditCategory.Content);
        await SeedAsync("report.resolve", AuditCategory.Moderation);

        var response = await _client.SendAsync(
            As(HttpMethod.Get, "/api/audit/moderation", ModeratorSubject, "moderator"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = await ReadAsync<List<AuditLogEntryDto>>(response);
        entries.Select(e => e.Action).Should().BeEquivalentTo(["post.delete", "report.resolve"]);
    }

    [Fact]
    public async Task ModerationTrail_IgnoresAFilterThatWouldWidenTheScope()
    {
        await SeedAsync("user.role.grant", AuditCategory.User, targetType: "user");
        await SeedAsync("post.delete", AuditCategory.Content, targetType: "post");

        var response = await _client.SendAsync(
            As(HttpMethod.Get, "/api/audit/moderation?targetType=user", ModeratorSubject, "moderator"));

        var entries = await ReadAsync<List<AuditLogEntryDto>>(response);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ModerationTrail_IsAlsoAvailableToAdministrators()
    {
        await SeedAsync("post.delete", AuditCategory.Content);

        var response = await _client.SendAsync(
            As(HttpMethod.Get, "/api/audit/moderation", AdminSubject, "administrator"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullTrail_HonoursTheLimitQueryParameter()
    {
        for (var i = 0; i < 5; i++)
            await SeedAsync($"action-{i}", AuditCategory.Content);

        var response = await _client.SendAsync(
            As(HttpMethod.Get, "/api/audit?limit=2", AdminSubject, "administrator"));

        (await ReadAsync<List<AuditLogEntryDto>>(response)).Should().HaveCount(2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static HttpRequestMessage As(HttpMethod method, string url, string subject, string roles)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.SubjectHeader, subject);
        request.Headers.Add(TestAuthHandler.RolesHeader, roles);
        return request;
    }

    private static Task<T> ReadAsync<T>(HttpResponseMessage response) => TestJson.ReadAsync<T>(response);

    private async Task SeedAsync(string action, AuditCategory category, string targetType = "user")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorId = Guid.NewGuid(),
            Action = action,
            TargetType = targetType,
            TargetId = "target-1",
            Category = category
        });

        await db.SaveChangesAsync();
    }
}
