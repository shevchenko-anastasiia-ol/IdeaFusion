using System.Net;
using System.Net.Http.Json;
using ContentBLL.DTO.Comment;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class CommentIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public CommentIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static CommentDto MakeComment(int id, int postId = 1) => new()
    {
        CommentId = id,
        PostId    = postId,
        Body      = "Test comment body",
        CreatedAt = DateTime.UtcNow,
    };

    // ── GET BY ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_Returns200()
    {
        _factory.CommentServiceMock
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeComment(1));

        var response = await _client.GetAsync("/api/comment/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CommentDto>();
        dto!.CommentId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _factory.CommentServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommentDto?)null);

        var response = await _client.GetAsync("/api/comment/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET BY POST ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPost_Returns200WithList()
    {
        _factory.CommentServiceMock
            .Setup(s => s.GetByPostIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommentDto> { MakeComment(10, 5), MakeComment(11, 5) });

        var response = await _client.GetAsync("/api/comment/by-post/5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<CommentDto>>();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByPost_EmptyList_Returns200()
    {
        _factory.CommentServiceMock
            .Setup(s => s.GetByPostIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommentDto>());

        var response = await _client.GetAsync("/api/comment/by-post/99");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET REPLIES ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReplies_Returns200()
    {
        _factory.CommentServiceMock
            .Setup(s => s.GetRepliesAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommentDto> { MakeComment(20) });

        var response = await _client.GetAsync("/api/comment/3/replies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201WithComment()
    {
        var created = MakeComment(42, 1);
        _factory.CommentServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CommentCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var dto = new CommentCreateDto
        {
            PostId       = 1,
            PostAuthorId = 2,
            Body         = "Hello world",
            CreatedBy    = "user1",
        };

        var response = await _client.PostAsJsonAsync("/api/comment", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CommentDto>();
        body!.CommentId.Should().Be(42);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns200WithUpdatedComment()
    {
        var updated = new CommentDto { CommentId = 7, PostId = 1, Body = "Updated body", CreatedAt = DateTime.UtcNow };
        _factory.CommentServiceMock
            .Setup(s => s.UpdateAsync(7, It.IsAny<CommentUpdateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await _client.PutAsJsonAsync("/api/comment/7",
            new CommentUpdateDto { Body = "Updated body", UpdatedBy = "user1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns204()
    {
        _factory.CommentServiceMock
            .Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/api/comment/5");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
