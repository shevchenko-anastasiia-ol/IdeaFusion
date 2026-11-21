using System.Net;
using System.Net.Http.Json;
using ContentBLL.DTO.SavedPost;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class SavedPostIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public SavedPostIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static SavedPostDto MakeSaved(int id, int postId = 1, int userId = 2) => new()
    {
        SavedPostId = id,
        PostId      = postId,
        UserId      = userId,
        PostTitle   = "Some Post",
        SavedAt     = DateTime.UtcNow,
    };

    // ── COUNT ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_Returns200WithNumber()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.CountByPostAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var response = await _client.GetAsync("/api/savedpost/count?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().Be(5);
    }

    // ── GET MINE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMine_Returns200WithList()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.GetMineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedPostDto> { MakeSaved(1, 10, 3), MakeSaved(2, 11, 3) });

        var response = await _client.GetAsync("/api/savedpost/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<SavedPostDto>>();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMine_EmptyList_Returns200()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.GetMineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedPostDto>());

        var response = await _client.GetAsync("/api/savedpost/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── EXISTS ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exists_WhenSaved_Returns200True()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _client.GetAsync("/api/savedpost/exists?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<bool>()).Should().BeTrue();
    }

    [Fact]
    public async Task Exists_WhenNotSaved_Returns200False()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _client.GetAsync("/api/savedpost/exists?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<bool>()).Should().BeFalse();
    }

    // ── SAVE ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_Returns200WithDto()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.SaveAsync(It.IsAny<SavedPostCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSaved(99, 1, 2));

        var response = await _client.PostAsJsonAsync("/api/savedpost",
            new SavedPostCreateDto { PostId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SavedPostDto>();
        body!.SavedPostId.Should().Be(99);
    }

    // ── UNSAVE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unsave_Returns204()
    {
        _factory.SavedPostServiceMock
            .Setup(s => s.UnsaveAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/api/savedpost?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
