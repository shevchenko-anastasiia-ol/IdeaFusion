using System.Net;
using System.Text.Json;
using ContentAPI.Middleware;
using ContentDomain.Exception;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Content.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int statusCode, string body)> InvokeWith(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/test";

        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            logger);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    // ── Status codes ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_NotFoundException_Returns404()
    {
        var (status, _) = await InvokeWith(new NotFoundException("not found"));

        status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invoke_BusinessConflictException_Returns409()
    {
        var (status, _) = await InvokeWith(new BusinessConflictException("conflict"));

        status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invoke_ValidationException_Returns400()
    {
        var (status, _) = await InvokeWith(new ValidationException("invalid"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invoke_UnhandledException_Returns500()
    {
        var (status, _) = await InvokeWith(new InvalidOperationException("boom"));

        status.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    // ── Titles ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_NotFoundException_TitleIsResourceNotFound()
    {
        var (_, body) = await InvokeWith(new NotFoundException("msg"));

        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Resource Not Found");
    }

    [Fact]
    public async Task Invoke_BusinessConflictException_TitleIsBusinessConflict()
    {
        var (_, body) = await InvokeWith(new BusinessConflictException("msg"));

        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Business Conflict");
    }

    [Fact]
    public async Task Invoke_ValidationException_TitleIsValidationError()
    {
        var (_, body) = await InvokeWith(new ValidationException("msg"));

        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
    }

    [Fact]
    public async Task Invoke_UnhandledException_TitleIsInternalServerError()
    {
        var (_, body) = await InvokeWith(new InvalidOperationException("msg"));

        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
    }

    // ── Detail contains exception message ─────────────────────────────────────

    [Fact]
    public async Task Invoke_AnyException_DetailIsExceptionMessage()
    {
        var (_, body) = await InvokeWith(new NotFoundException("resource xyz not found"));

        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("detail").GetString().Should().Be("resource xyz not found");
    }

    // ── Content-Type ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_AnyException_ContentTypeIsApplicationJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("x"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
    }

    // ── Instance is the request path ──────────────────────────────────────────

    [Fact]
    public async Task Invoke_AnyException_InstanceIsRequestPath()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/posts/42";

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("x"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var doc  = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("instance").GetString().Should().Be("/api/posts/42");
    }

    // ── Response body is valid JSON ───────────────────────────────────────────

    [Fact]
    public async Task Invoke_AnyException_BodyIsValidJson()
    {
        var (_, body) = await InvokeWith(new BusinessConflictException("err"));

        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow();
    }

    // ── Logger is called for unhandled exceptions ─────────────────────────────

    [Fact]
    public async Task Invoke_UnhandledException_LogsError()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var context    = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            loggerMock.Object);

        await middleware.InvokeAsync(context);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_KnownException_DoesNotLogError()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var context    = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("not found"),
            loggerMock.Object);

        await middleware.InvokeAsync(context);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ── Next delegate is not called after exception ───────────────────────────

    [Fact]
    public async Task Invoke_NoException_DoesNotSetErrorStatusCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }
}

public class GlobalExceptionHandlingMiddlewareExtensionsTests
{
    [Fact]
    public void UseGlobalExceptionHandling_ReturnsSameBuilder()
    {
        var app = new ApplicationBuilder(null!);

        var result = app.UseGlobalExceptionHandling();

        result.Should().BeSameAs(app);
    }
}
