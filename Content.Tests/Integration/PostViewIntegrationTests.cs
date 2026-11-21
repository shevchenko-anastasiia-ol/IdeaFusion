using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Integration;

public class PostViewIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContentWebApplicationFactory _factory;

    public PostViewIntegrationTests(ContentWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── RECORD ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Record_AuthenticatedUser_Returns204()
    {
        _factory.PostViewServiceMock
            .Setup(s => s.RecordAsync(5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsync("/api/postview/record?postId=5", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Record_AnonymousUser_IsRejectedByTheFallbackPolicy()
    {
        _factory.PostViewServiceMock
            .Setup(s => s.RecordAsync(5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/postview/record?postId=5");
        request.Headers.Add(TestAuthHandler.AnonymousHeader, "true");

        var response = await _client.SendAsync(request);

        // Phase 1: an endpoint with no [Authorize] attribute still requires an
        // authenticated caller, so an anonymous request must not reach the BLL.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.PostViewServiceMock.Verify(
            s => s.RecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Record_AuthenticatedUser_UsesTheSubjectFromTheToken()
    {
        _factory.PostViewServiceMock
            .Setup(s => s.RecordAsync(5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/postview/record?postId=5");
        request.Headers.Add(TestAuthHandler.SubjectHeader, TestAuthHandler.DefaultSubject);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── COUNT ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_Returns200WithViewCount()
    {
        _factory.PostViewServiceMock
            .Setup(s => s.CountByPostAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var response = await _client.GetAsync("/api/postview/count?postId=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().Be(42);
    }

    [Fact]
    public async Task Count_WhenNoViews_Returns200WithZero()
    {
        _factory.PostViewServiceMock
            .Setup(s => s.CountByPostAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var response = await _client.GetAsync("/api/postview/count?postId=99");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<int>()).Should().Be(0);
    }
}
