using System.Net;
using System.Net.Http.Json;
using ContentBLL.DTO.Tag;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class TagIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public TagIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static TagDto MakeTag(int id, string name = "dotnet") =>
        new() { TagId = id, Name = name };

    // ── GET ALL ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        _factory.TagServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagDto> { MakeTag(1, "csharp"), MakeTag(2, "dotnet") });

        var response = await _client.GetAsync("/api/tag");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
        tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_EmptyList_Returns200()
    {
        _factory.TagServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagDto>());

        var response = await _client.GetAsync("/api/tag");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET BY ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_Returns200()
    {
        _factory.TagServiceMock
            .Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTag(3, "azure"));

        var response = await _client.GetAsync("/api/tag/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        tag!.Name.Should().Be("azure");
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _factory.TagServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TagDto?)null);

        var response = await _client.GetAsync("/api/tag/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET BY POST ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPost_Returns200()
    {
        _factory.TagServiceMock
            .Setup(s => s.GetByPostIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagDto> { MakeTag(1, "api"), MakeTag(2, "rest") });

        var response = await _client.GetAsync("/api/tag/by-post/10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
        tags.Should().HaveCount(2);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201WithTag()
    {
        _factory.TagServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<TagCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTag(55, "newTag"));

        var response = await _client.PostAsJsonAsync("/api/tag", new TagCreateDto { Name = "newTag" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        tag!.Name.Should().Be("newTag");
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns204()
    {
        _factory.TagServiceMock
            .Setup(s => s.DeleteAsync(4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/api/tag/4");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
