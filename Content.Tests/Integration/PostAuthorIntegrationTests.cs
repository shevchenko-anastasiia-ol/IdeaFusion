using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    // ── GET BY USERNAME ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_ExistingUser_Returns200WithBothIds()
    {
        _factory.PostServiceMock
            .Setup(s => s.EnsurePostAuthorAsync("Alice", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 2, UserId: 7));

        var response = await _client.GetAsync("/api/postauthor/by-username/Alice");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("postAuthorId").GetInt32().Should().Be(2);
        body.GetProperty("userId").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetByUserName_WithAvatarUrl_Returns200()
    {
        _factory.PostServiceMock
            .Setup(s => s.EnsurePostAuthorAsync(
                "Bob",
                "http://example.com/avatar.png",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 3, UserId: 12));

        var response = await _client.GetAsync(
            "/api/postauthor/by-username/Bob?avatarUrl=http://example.com/avatar.png");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("userId").GetInt32().Should().Be(12);
    }

    [Fact]
    public async Task GetByUserName_NewUser_CreatesAndReturnsIds()
    {
        _factory.PostServiceMock
            .Setup(s => s.EnsurePostAuthorAsync("NewUser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 5, UserId: 99));

        var response = await _client.GetAsync("/api/postauthor/by-username/NewUser");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("postAuthorId").GetInt32().Should().Be(5);
        body.GetProperty("userId").GetInt32().Should().Be(99);
    }
}
