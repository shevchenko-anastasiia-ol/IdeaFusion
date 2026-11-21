using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class PostAuthorIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public PostAuthorIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static ResolvedPostAuthor Author(int postAuthorId, int userId, string userName) =>
        new(postAuthorId, userId, Guid.Empty, userName, null, false);

    // ── GET ME ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_Returns200WithBothIds()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Author(2, 7, "Alice"));

        var response = await _client.GetAsync("/api/postauthor/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("postAuthorId").GetInt32().Should().Be(2);
        body.GetProperty("userId").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetMe_ReportsWhetherTheRowWasJustCreated()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPostAuthor(5, 99, Guid.Empty, "NewUser", null, true));

        var response = await _client.GetAsync("/api/postauthor/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("created").GetBoolean().Should().BeTrue();
    }

    // ── PATCH ME / AVATAR ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOwnAvatar_WithAvatarUrl_Returns200()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.ResolveForWriteAsync(
                "http://example.com/avatar.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPostAuthor(3, 12, Guid.Empty, "Bob",
                "http://example.com/avatar.png", false));

        var response = await _client.PatchAsync(
            "/api/postauthor/me/avatar?avatarUrl=http://example.com/avatar.png", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("userId").GetInt32().Should().Be(12);
        body.GetProperty("avatarUrl").GetString().Should().Be("http://example.com/avatar.png");
    }

    // ── GET BY USERNAME ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_ExistingUser_Returns200WithBothIds()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.FindByUserNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Author(2, 7, "Alice"));

        var response = await _client.GetAsync("/api/postauthor/by-username/Alice");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("postAuthorId").GetInt32().Should().Be(2);
        body.GetProperty("userId").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetByUserName_UnknownUser_Returns404()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.FindByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedPostAuthor?)null);

        var response = await _client.GetAsync("/api/postauthor/by-username/Ghost");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByUserName_DoesNotCreateARow()
    {
        _factory.PostAuthorResolverMock
            .Setup(r => r.FindByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Author(5, 99, "NewUser"));

        var response = await _client.GetAsync("/api/postauthor/by-username/NewUser");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.PostAuthorResolverMock.Verify(
            r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
