using System.Net;
using Collaboration.API.Middleware;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Collaboration.Application.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<int> InvokeAsync(Exception? thrownException, bool cancelRequest = false)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (cancelRequest)
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            context.RequestAborted = cts.Token;
        }

        RequestDelegate next = _ => thrownException is null
            ? Task.CompletedTask
            : Task.FromException(thrownException);

        var logger = NullLogger<GlobalExceptionMiddleware>.Instance;
        var middleware = new GlobalExceptionMiddleware(next, logger);

        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    // ── No exception ──────────────────────────────────────────────────────────

    [Fact]
    public async Task NoException_PassesThrough_WithoutChangingStatus()
    {
        var status = await InvokeAsync(null);

        status.Should().Be(200);
    }

    // ── Client disconnected ───────────────────────────────────────────────────

    [Fact]
    public async Task OperationCanceled_WhenRequestAborted_Returns499()
    {
        var status = await InvokeAsync(new OperationCanceledException(), cancelRequest: true);

        status.Should().Be(499);
    }

    // ── Domain exceptions ─────────────────────────────────────────────────────

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var status = await InvokeAsync(new NotFoundException("not found"));

        status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConflictException_Returns409()
    {
        var status = await InvokeAsync(new ConflictException("conflict"));

        status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DomainException_Returns400()
    {
        var status = await InvokeAsync(new DomainException("bad"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    // ── Validation exceptions ─────────────────────────────────────────────────

    [Fact]
    public async Task CustomValidationException_Returns400()
    {
        var failures = new[] { new ValidationFailure("Name", "Required") };
        var status = await InvokeAsync(new CustomValidationException(failures));

        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FluentValidationException_Returns400()
    {
        var failures = new[] { new ValidationFailure("Field", "Invalid") };
        var status = await InvokeAsync(new FluentValidation.ValidationException(failures));

        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    // ── Concurrency exception ─────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrencyException_Returns412()
    {
        var status = await InvokeAsync(new ConcurrencyException("entity-1", 1, 2));

        status.Should().Be(412);
    }

    // ── MongoDB exceptions ────────────────────────────────────────────────────

    [Fact]
    public async Task MongoDbConnectionException_Returns503()
    {
        var status = await InvokeAsync(new MongoDbConnectionException("db down"));

        status.Should().Be((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task MongoDbWriteException_Returns500()
    {
        var status = await InvokeAsync(new MongoDbWriteException("write failed"));

        status.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    // ── Standard exceptions ───────────────────────────────────────────────────

    [Fact]
    public async Task ArgumentException_Returns400()
    {
        var status = await InvokeAsync(new ArgumentException("bad arg"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var status = await InvokeAsync(new KeyNotFoundException("missing key"));

        status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidOperationException_Returns409()
    {
        var status = await InvokeAsync(new InvalidOperationException("invalid op"));

        status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns403()
    {
        var status = await InvokeAsync(new UnauthorizedAccessException());

        status.Should().Be((int)HttpStatusCode.Forbidden);
    }

    // ── Default (unknown exception) ───────────────────────────────────────────

    [Fact]
    public async Task UnknownException_Returns500()
    {
        var status = await InvokeAsync(new Exception("boom"));

        status.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    // ── Content-Type header ───────────────────────────────────────────────────

    [Fact]
    public async Task AnyException_SetsContentTypeToApplicationProblemJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => Task.FromException(new DomainException("err"));
        var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/problem+json");
    }
}