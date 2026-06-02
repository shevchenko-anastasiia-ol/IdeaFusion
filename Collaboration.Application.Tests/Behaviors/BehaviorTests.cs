using Collaboration.Application.Behaviors;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Behaviors;

// ── Shared test types ─────────────────────────────────────────────────────────

public record SomeQuery;        // name ends with "Query"  → CachingBehavior caches it
public record SomeCommand;      // name does not end with "Query" → not cached
public record SomeResponse(string Value = "ok");

// ── CachingBehaviorTests ──────────────────────────────────────────────────────

public class CachingBehaviorTests
{
    private static IMemoryCache CreateCache() =>
        new MemoryCache(new MemoryCacheOptions());

    private static string CacheKey<T>(T request) where T : notnull =>
        $"{typeof(T).FullName}-{request.GetHashCode()}";

    // ── Cache hit ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValueWithoutCallingNext()
    {
        var cache = CreateCache();
        var behavior = new CachingBehavior<SomeQuery, SomeResponse>(cache);
        var request = new SomeQuery();
        var cached = new SomeResponse("cached");
        cache.Set(CacheKey(request), cached);

        var nextCalled = false;
        RequestHandlerDelegate<SomeResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new SomeResponse("fresh"));
        };

        var result = await behavior.Handle(request, next, default);

        result.Should().Be(cached);
        nextCalled.Should().BeFalse();
    }

    // ── Cache miss + Query type → response is cached ──────────────────────────

    [Fact]
    public async Task Handle_CacheMiss_QueryRequest_CallsNextAndCachesResponse()
    {
        var cache = CreateCache();
        var behavior = new CachingBehavior<SomeQuery, SomeResponse>(cache);
        var request = new SomeQuery();
        var fresh = new SomeResponse("fresh");

        var result = await behavior.Handle(request, _ => Task.FromResult(fresh), default);

        result.Should().Be(fresh);
        cache.TryGetValue(CacheKey(request), out SomeResponse? stored).Should().BeTrue();
        stored.Should().Be(fresh);
    }

    // ── Cache miss + non-Query type → response is NOT cached ─────────────────

    [Fact]
    public async Task Handle_CacheMiss_CommandRequest_CallsNextWithoutCaching()
    {
        var cache = CreateCache();
        var behavior = new CachingBehavior<SomeCommand, SomeResponse>(cache);
        var request = new SomeCommand();
        var fresh = new SomeResponse("fresh");

        var result = await behavior.Handle(request, _ => Task.FromResult(fresh), default);

        result.Should().Be(fresh);
        cache.TryGetValue(CacheKey(request), out SomeResponse? _).Should().BeFalse();
    }
}

// ── ExceptionHandlingBehaviorTests ────────────────────────────────────────────

public class ExceptionHandlingBehaviorTests
{
    private static ExceptionHandlingBehavior<SomeQuery, SomeResponse> Build() =>
        new(NullLogger<ExceptionHandlingBehavior<SomeQuery, SomeResponse>>.Instance);

    private static RequestHandlerDelegate<SomeResponse> Throws(Exception ex) =>
        _ => Task.FromException<SomeResponse>(ex);

    private static RequestHandlerDelegate<SomeResponse> Returns(SomeResponse r) =>
        _ => Task.FromResult(r);

    // ── No exception ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoException_ReturnsResponse()
    {
        var expected = new SomeResponse("ok");
        var result = await Build().Handle(new SomeQuery(), Returns(expected), default);
        result.Should().Be(expected);
    }

    // ── DomainException ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DomainException_LogsAndRethrows()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new ExceptionHandlingBehavior<SomeQuery, SomeResponse>(loggerMock.Object);
        var ex = new DomainException("bad domain");

        var act = () => behavior.Handle(new SomeQuery(), Throws(ex), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("bad domain");
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Domain validation failed")),
                ex,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── ValidationException ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidationException_LogsWarningPerFailureAndRethrows()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new ExceptionHandlingBehavior<SomeQuery, SomeResponse>(loggerMock.Object);
        var failures = new[]
        {
            new ValidationFailure("Name", "Required"),
            new ValidationFailure("Age", "Must be positive")
        };
        var vex = new FluentValidation.ValidationException(failures);

        var act = () => behavior.Handle(new SomeQuery(), Throws(vex), default);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Validation failure")),
                vex,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    // ── Generic exception ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_GenericException_LogsErrorAndRethrows()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new ExceptionHandlingBehavior<SomeQuery, SomeResponse>(loggerMock.Object);
        var ex = new InvalidOperationException("unexpected");

        var act = () => behavior.Handle(new SomeQuery(), Throws(ex), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unexpected");
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Unhandled exception")),
                ex,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

// ── LoggingBehaviorTests ──────────────────────────────────────────────────────

public class LoggingBehaviorTests
{
    // ── Success path covers all lines (no conditional branches) ──────────────

    [Fact]
    public async Task Handle_ReturnsResponseAndLogsStartAndSuccess()
    {
        var loggerMock = new Mock<ILogger<LoggingBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new LoggingBehavior<SomeQuery, SomeResponse>(loggerMock.Object);
        var expected = new SomeResponse("val");

        var result = await behavior.Handle(
            new SomeQuery(),
            _ => Task.FromResult(expected),
            default);

        result.Should().Be(expected);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("[START]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("[SUCCESS]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_PropagatesException()
    {
        var behavior = new LoggingBehavior<SomeQuery, SomeResponse>(
            NullLogger<LoggingBehavior<SomeQuery, SomeResponse>>.Instance);

        var act = () => behavior.Handle(
            new SomeQuery(),
            _ => Task.FromException<SomeResponse>(new DomainException("err")),
            default);

        await act.Should().ThrowAsync<DomainException>();
    }
}

// ── PerformanceBehaviorTests ──────────────────────────────────────────────────

public class PerformanceBehaviorTests
{
    // ── Fast request (≤ 500 ms) → LogInformation ─────────────────────────────

    [Fact]
    public async Task Handle_FastRequest_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<PerformanceBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new PerformanceBehavior<SomeQuery, SomeResponse>(loggerMock.Object);
        var expected = new SomeResponse("fast");

        var result = await behavior.Handle(
            new SomeQuery(),
            _ => Task.FromResult(expected),
            default);

        result.Should().Be(expected);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("[PERFORMANCE]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Slow request (> 500 ms) → LogWarning ─────────────────────────────────

    [Fact]
    public async Task Handle_SlowRequest_LogsWarning()
    {
        var loggerMock = new Mock<ILogger<PerformanceBehavior<SomeQuery, SomeResponse>>>();
        var behavior = new PerformanceBehavior<SomeQuery, SomeResponse>(loggerMock.Object);

        var result = await behavior.Handle(
            new SomeQuery(),
            async _ =>
            {
                await Task.Delay(600);
                return new SomeResponse("slow");
            },
            default);

        result.Value.Should().Be("slow");
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("[PERFORMANCE]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

// ── ValidationBehaviorTests ───────────────────────────────────────────────────

public class ValidationBehaviorTests
{
    private static ValidationBehavior<SomeQuery, SomeResponse> Build(
        params IValidator<SomeQuery>[] validators) =>
        new(validators);

    private static RequestHandlerDelegate<SomeResponse> OkNext() =>
        _ => Task.FromResult(new SomeResponse("ok"));

    // ── No validators registered → calls next directly ───────────────────────

    [Fact]
    public async Task Handle_NoValidators_CallsNextAndReturnsResponse()
    {
        var behavior = Build();
        var nextCalled = false;
        RequestHandlerDelegate<SomeResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new SomeResponse("ok"));
        };

        var result = await behavior.Handle(new SomeQuery(), next, default);

        result.Value.Should().Be("ok");
        nextCalled.Should().BeTrue();
    }

    // ── Single validator, no errors → calls next ─────────────────────────────

    [Fact]
    public async Task Handle_ValidatorPassess_CallsNext()
    {
        var validatorMock = new Mock<IValidator<SomeQuery>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
                     .ReturnsAsync(new ValidationResult());

        var behavior = Build(validatorMock.Object);
        var nextCalled = false;
        RequestHandlerDelegate<SomeResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new SomeResponse("ok"));
        };

        var result = await behavior.Handle(new SomeQuery(), next, default);

        result.Value.Should().Be("ok");
        nextCalled.Should().BeTrue();
    }

    // ── Single validator, has errors → throws ValidationException ────────────

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationException()
    {
        var failure = new ValidationFailure("Name", "Required");
        var validatorMock = new Mock<IValidator<SomeQuery>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
                     .ReturnsAsync(new ValidationResult(new[] { failure }));

        var behavior = Build(validatorMock.Object);

        var act = () => behavior.Handle(new SomeQuery(), OkNext(), default);

        var ex = await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        ex.Which.Errors.Should().ContainSingle()
          .Which.PropertyName.Should().Be("Name");
    }

    // ── Multiple validators, all errors collected ─────────────────────────────

    [Fact]
    public async Task Handle_MultipleValidators_CollectsAllFailures()
    {
        var f1 = new ValidationFailure("Name", "Required");
        var f2 = new ValidationFailure("Age", "Must be positive");

        var v1 = new Mock<IValidator<SomeQuery>>();
        v1.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
          .ReturnsAsync(new ValidationResult(new[] { f1 }));

        var v2 = new Mock<IValidator<SomeQuery>>();
        v2.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
          .ReturnsAsync(new ValidationResult(new[] { f2 }));

        var behavior = Build(v1.Object, v2.Object);

        var act = () => behavior.Handle(new SomeQuery(), OkNext(), default);

        var ex = await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
    }

    // ── Multiple validators, some pass, some fail ────────────────────────────

    [Fact]
    public async Task Handle_SomeValidatorsPassSomeFail_ThrowsWithOnlyFailures()
    {
        var failure = new ValidationFailure("Email", "Invalid");

        var passing = new Mock<IValidator<SomeQuery>>();
        passing.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
               .ReturnsAsync(new ValidationResult());

        var failing = new Mock<IValidator<SomeQuery>>();
        failing.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SomeQuery>>(), default))
               .ReturnsAsync(new ValidationResult(new[] { failure }));

        var behavior = Build(passing.Object, failing.Object);

        var act = () => behavior.Handle(new SomeQuery(), OkNext(), default);

        var ex = await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        ex.Which.Errors.Should().ContainSingle()
          .Which.PropertyName.Should().Be("Email");
    }
}