using System.Net;
using System.Net.Http.Json;
using ContentBLL.DTO.Post;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

/// <summary>
/// Integration tests for PostController — validate HTTP routing, serialisation,
/// and status-code mapping through the full ASP.NET Core pipeline.
/// IPostService is mocked so no real database or MinIO is required.
/// </summary>
public class PostIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public PostIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static PostDto MakePost(int id, string title = "Test Post") => new()
    {
        PostId    = id,
        Title     = title,
        Status    = "Published",
        CreatedAt = DateTime.UtcNow,
        Tags      = new List<string>(),
        MediaUrls = new List<string>()
    };

    // ─── GET ALL ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithEmptyList()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto>());

        var response = await _client.GetAsync("/api/post");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
        posts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAll_Returns200WithPosts()
    {
        var seeded = new List<PostDto> { MakePost(1, "Alpha"), MakePost(2, "Beta") };
        _factory.PostServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        var response = await _client.GetAsync("/api/post");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
        posts.Should().HaveCount(2);
        posts![0].Title.Should().Be("Alpha");
    }

    // ─── GET BY ID ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_Returns200()
    {
        var post = MakePost(42, "Found Post");
        _factory.PostServiceMock
            .Setup(s => s.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var response = await _client.GetAsync("/api/post/42");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PostDto>();
        result!.PostId.Should().Be(42);
        result.Title.Should().Be("Found Post");
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostDto?)null);

        var response = await _client.GetAsync("/api/post/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── GET BY AUTHOR ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAuthor_Returns200WithList()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetByAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto> { MakePost(10) });

        var response = await _client.GetAsync("/api/post/by-author/5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
        posts.Should().HaveCount(1);
    }

    // ─── GET BY USER ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserId_Returns200()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetByUserIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto> { MakePost(3) });

        var response = await _client.GetAsync("/api/post/by-userid/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET BY COLLABORATION ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByCollaboration_Returns200()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetByCollaborationAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto>());

        var response = await _client.GetAsync("/api/post/by-collaboration/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET BY TAG ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTag_Returns200()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetByTagAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto> { MakePost(8) });

        var response = await _client.GetAsync("/api/post/by-tag/2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingPost_Returns204()
    {
        _factory.PostServiceMock
            .Setup(s => s.DeleteAsync(55, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/api/post/55");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─── ARCHIVE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Archive_ExistingPost_Returns204()
    {
        _factory.PostServiceMock
            .Setup(s => s.ArchiveAsync(11, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.PatchAsync("/api/post/11/archive", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─── CONTENT NEGOTIATION ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ResponseContentType_IsApplicationJson()
    {
        _factory.PostServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostDto>());

        var response = await _client.GetAsync("/api/post");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
