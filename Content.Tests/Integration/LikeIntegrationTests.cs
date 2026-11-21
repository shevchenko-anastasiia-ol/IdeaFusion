using System.Net;
using System.Net.Http.Json;
using ContentBLL.DTO.Like;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class LikeIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public LikeIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── COUNT ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_Returns200WithNumber()
    {
        _factory.LikeServiceMock
            .Setup(s => s.CountByPostAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var response = await _client.GetAsync("/api/like/count?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().Be(7);
    }

    [Fact]
    public async Task Count_WhenZero_Returns200WithZero()
    {
        _factory.LikeServiceMock
            .Setup(s => s.CountByPostAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var response = await _client.GetAsync("/api/like/count?postId=99");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().Be(0);
    }

    // ── EXISTS ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exists_WhenLiked_Returns200True()
    {
        _factory.LikeServiceMock
            .Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _client.GetAsync("/api/like/exists?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var exists = await response.Content.ReadFromJsonAsync<bool>();
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Exists_WhenNotLiked_Returns200False()
    {
        _factory.LikeServiceMock
            .Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _client.GetAsync("/api/like/exists?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var exists = await response.Content.ReadFromJsonAsync<bool>();
        exists.Should().BeFalse();
    }

    // ── ADD ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_Returns200WithLike()
    {
        var like = new LikeDto { LikeId = 1, PostId = 1, UserId = 2, CreatedAt = DateTime.UtcNow };
        _factory.LikeServiceMock
            .Setup(s => s.AddAsync(It.IsAny<LikeCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(like);

        var response = await _client.PostAsJsonAsync("/api/like",
            new LikeCreateDto { PostId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LikeDto>();
        body!.PostId.Should().Be(1);
    }

    // ── REMOVE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_Returns204()
    {
        _factory.LikeServiceMock
            .Setup(s => s.RemoveAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/api/like?postId=1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
