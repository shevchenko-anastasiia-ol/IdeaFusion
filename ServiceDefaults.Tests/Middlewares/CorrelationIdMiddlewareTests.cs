using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ServiceDefaults.Tests.Middlewares;

public class CorrelationIdMiddlewareTests
{
    private const string CorrelationHeader = "X-Correlation-Id";

    private readonly Mock<ILogger<CorrelationIdMiddleware>> _loggerMock = new();

    private CorrelationIdMiddleware Create(RequestDelegate next) =>
        new(next, _loggerMock.Object);

    // ── existing correlation ID ───────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WithExistingCorrelationId_SetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeader] = "existing-id";
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationHeader].ToString().Should().Be("existing-id");
    }

    [Fact]
    public async Task InvokeAsync_WithExistingCorrelationId_SetsItemsEntry()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeader] = "my-id";
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Items[CorrelationHeader].Should().Be("my-id");
    }

    // ── no correlation ID ─────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_GeneratesGuidInResponseHeader()
    {
        var context    = new DefaultHttpContext();
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers[CorrelationHeader].ToString();
        Guid.TryParse(value, out _).Should().BeTrue("a valid GUID should be generated");
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceCorrelationId_GeneratesGuid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeader] = "   ";
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers[CorrelationHeader].ToString();
        Guid.TryParse(value, out _).Should().BeTrue();
    }

    // ── success path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_SuccessfulNext_DoesNotThrow()
    {
        var context    = new DefaultHttpContext();
        var middleware = Create(_ => Task.CompletedTask);

        var act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }

    // ── OperationCanceledException swallowed ──────────────────────────────────

    [Fact]
    public async Task InvokeAsync_OperationCanceledWithRequestAborted_DoesNotRethrow()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new DefaultHttpContext();
        context.RequestAborted = cts.Token;

        var middleware = Create(_ => throw new OperationCanceledException());

        var act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }

    // ── generic exception rethrown ────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_GenericException_Rethrows()
    {
        var context    = new DefaultHttpContext();
        var middleware = Create(_ => throw new InvalidOperationException("test failure"));

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test failure");
    }

    [Fact]
    public async Task InvokeAsync_TaskException_Rethrows()
    {
        var context    = new DefaultHttpContext();
        var middleware = Create(_ => Task.FromException(new ArgumentException("bad argument")));

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("bad argument");
    }
}
