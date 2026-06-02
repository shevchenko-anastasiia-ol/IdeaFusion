using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ServiceDefaults.Http;
using System.Net;
using Xunit;

namespace ServiceDefaults.Tests.Http;

public class CorrelationIdHandlerTests
{
    private const string CorrelationHeader = "X-Correlation-Id";

    private static (CorrelationIdHandler handler, List<HttpRequestMessage> captured)
        BuildHandler(IHttpContextAccessor accessor)
    {
        var captured    = new List<HttpRequestMessage>();
        var innerHandler = new CaptureHandler(req =>
        {
            captured.Add(req);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler      = new CorrelationIdHandler(accessor);
        handler.InnerHandler = innerHandler;
        return (handler, captured);
    }

    // ── with existing correlation ID ──────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithCorrelationIdInContext_ForwardsHeaderValue()
    {
        var correlationId = "abc-123-xyz";
        var accessor      = MakeAccessor(correlationId);
        var (handler, captured) = BuildHandler(accessor);

        await new HttpClient(handler).GetAsync("http://example.com/");

        captured[0].Headers.Should().ContainKey(CorrelationHeader);
        captured[0].Headers.GetValues(CorrelationHeader).First().Should().Be(correlationId);
    }

    [Fact]
    public async Task SendAsync_WithCorrelationIdInContext_DoesNotAddSecondHeader()
    {
        var accessor        = MakeAccessor("id-1");
        var (handler, captured) = BuildHandler(accessor);

        await new HttpClient(handler).GetAsync("http://example.com/");

        captured[0].Headers.GetValues(CorrelationHeader).Should().HaveCount(1);
    }

    // ── without context (null HttpContext) ───────────────────────────────────

    [Fact]
    public async Task SendAsync_NullHttpContext_AddsGeneratedGuidHeader()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var (handler, captured) = BuildHandler(accessorMock.Object);

        await new HttpClient(handler).GetAsync("http://example.com/");

        captured[0].Headers.Should().ContainKey(CorrelationHeader);
        var value = captured[0].Headers.GetValues(CorrelationHeader).First();
        Guid.TryParse(value, out _).Should().BeTrue("fallback should be a valid GUID");
    }

    // ── forwarded correctly ───────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ReturnsResponseFromInnerHandler()
    {
        var accessor        = MakeAccessor("id-99");
        var (handler, _)    = BuildHandler(accessor);

        var response = await new HttpClient(handler).GetAsync("http://example.com/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void CorrelationIdHandler_InheritsFromDelegatingHandler()
    {
        typeof(CorrelationIdHandler).IsAssignableTo(typeof(DelegatingHandler)).Should().BeTrue();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IHttpContextAccessor MakeAccessor(string correlationId)
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationHeader] = correlationId;

        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(a => a.HttpContext).Returns(context);
        return mock.Object;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
