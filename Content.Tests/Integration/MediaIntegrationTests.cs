using System.Net;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Integration;

public class MediaIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MediaIntegrationTests(ContentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── UPLOAD ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        // Send multipart form with no file part
        var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/media/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_ValidFile_Returns200WithUrl()
    {
        // MinIO is mocked — all calls are no-ops, URL is built from config
        using var ms = new MemoryStream("fake image bytes"u8.ToArray());
        var fileContent = new StreamContent(ms);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.jpg");

        var response = await _client.PostAsync("/api/media/upload", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var url = await response.Content.ReadAsStringAsync();
        url.Should().Contain("localhost:9000");
    }
}
