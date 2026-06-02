using Collaboration.API.Controllers;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.API;

public class BaseApiControllerTests
{
    private readonly TestableController _controller;
    private readonly DefaultHttpContext _httpContext;

    public BaseApiControllerTests()
    {
        var mediator = new Mock<IMediator>();
        _httpContext = new DefaultHttpContext();
        _controller = new TestableController(mediator.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private class TestableController : BaseApiController
    {
        public TestableController(IMediator mediator) : base(mediator) { }
        public new IActionResult HandleException(Exception ex) => base.HandleException(ex);
        public new string GenerateETag(DateTime ts) => base.GenerateETag(ts);
        public new bool ValidateETag(string? requestETag, string currentETag) => base.ValidateETag(requestETag, currentETag);
        public new void AddETagHeader(string etag) => base.AddETagHeader(etag);
        public new string? GetIfMatchHeader() => base.GetIfMatchHeader();
        public new IActionResult HandleConcurrencyConflict(ConcurrencyException ex) => base.HandleConcurrencyConflict(ex);
    }

    // ── HandleException ───────────────────────────────────────────────────────

    [Fact]
    public void HandleException_DomainException_ReturnsBadRequest()
    {
        var result = _controller.HandleException(new DomainException("bad"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void HandleException_NotFoundException_ReturnsNotFound()
    {
        var result = _controller.HandleException(new NotFoundException("not found"));
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void HandleException_ConflictException_ReturnsConflict()
    {
        var result = _controller.HandleException(new ConflictException("conflict"));
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public void HandleException_ArgumentException_ReturnsBadRequest()
    {
        var result = _controller.HandleException(new ArgumentException("bad arg"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void HandleException_KeyNotFoundException_ReturnsNotFound()
    {
        var result = _controller.HandleException(new KeyNotFoundException("missing"));
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void HandleException_InvalidOperationException_ReturnsConflict()
    {
        var result = _controller.HandleException(new InvalidOperationException("conflict"));
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public void HandleException_UnauthorizedAccessException_Returns403()
    {
        var result = _controller.HandleException(new UnauthorizedAccessException());
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void HandleException_ConcurrencyException_Returns409()
    {
        var result = _controller.HandleException(new ConcurrencyException("entity-1", 1, 2));
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public void HandleException_MongoDbConnectionException_Returns503()
    {
        var result = _controller.HandleException(new MongoDbConnectionException("db down"));
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(503);
    }

    [Fact]
    public void HandleException_MongoDbWriteException_Returns500()
    {
        var result = _controller.HandleException(new MongoDbWriteException("write fail"));
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public void HandleException_GenericException_Returns500()
    {
        var result = _controller.HandleException(new Exception("oops"));
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(500);
    }

    // ── ETag helpers ──────────────────────────────────────────────────────────

    [Fact]
    public void GenerateETag_ReturnsQuotedTicks()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var etag = _controller.GenerateETag(dt);
        etag.Should().Be($"\"{dt.Ticks}\"");
    }

    [Fact]
    public void ValidateETag_WhenRequestETagIsNull_ReturnsTrue()
    {
        _controller.ValidateETag(null, "\"12345\"").Should().BeTrue();
    }

    [Fact]
    public void ValidateETag_WhenRequestETagIsEmpty_ReturnsTrue()
    {
        _controller.ValidateETag("", "\"12345\"").Should().BeTrue();
    }

    [Fact]
    public void ValidateETag_WhenTagsMatch_ReturnsTrue()
    {
        _controller.ValidateETag("\"12345\"", "\"12345\"").Should().BeTrue();
    }

    [Fact]
    public void ValidateETag_WhenTagsDiffer_ReturnsFalse()
    {
        _controller.ValidateETag("\"11111\"", "\"22222\"").Should().BeFalse();
    }

    // ── HandleException – validation branches ─────────────────────────────────

    [Fact]
    public void HandleException_CustomValidationException_ReturnsBadRequest()
    {
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Name", "Name is required")
        };
        var result = _controller.HandleException(new CustomValidationException(failures));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void HandleException_ValidationException_ReturnsBadRequest()
    {
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is invalid")
        };
        var result = _controller.HandleException(new ValidationException(failures));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── AddETagHeader ─────────────────────────────────────────────────────────

    [Fact]
    public void AddETagHeader_AddsETagToResponseHeaders()
    {
        _controller.AddETagHeader("\"99999\"");
        _httpContext.Response.Headers["ETag"].ToString().Should().Be("\"99999\"");
    }

    // ── GetIfMatchHeader ──────────────────────────────────────────────────────

    [Fact]
    public void GetIfMatchHeader_WhenHeaderPresent_ReturnsValue()
    {
        _httpContext.Request.Headers["If-Match"] = "\"abc123\"";
        _controller.GetIfMatchHeader().Should().Be("\"abc123\"");
    }

    [Fact]
    public void GetIfMatchHeader_WhenHeaderAbsent_ReturnsNull()
    {
        _controller.GetIfMatchHeader().Should().BeNull();
    }

    // ── HandleConcurrencyConflict ─────────────────────────────────────────────

    [Fact]
    public void HandleConcurrencyConflict_Returns412WithDetails()
    {
        var ex = new ConcurrencyException("entity-42", 3, 5);
        var result = _controller.HandleConcurrencyConflict(ex);
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(412);
    }
}