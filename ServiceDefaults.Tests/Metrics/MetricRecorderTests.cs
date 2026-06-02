using FluentAssertions;
using ServiceDefaults.Metrics;
using System.Diagnostics.Metrics;
using Xunit;

namespace ServiceDefaults.Tests.Metrics;

public class MetricRecorderTests
{
    private static Histogram<double> NewHistogram()
    {
        var meter = new Meter($"MetricRecorder.Tests.{Guid.NewGuid()}", "1.0.0");
        return meter.CreateHistogram<double>("test_latency_seconds");
    }

    // ── void overload ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordOperationAsync_Void_ExecutesFunc()
    {
        var called = false;
        await MetricRecorder.RecordOperationAsync(NewHistogram(), "op", () =>
        {
            called = true;
            return Task.CompletedTask;
        });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task RecordOperationAsync_Void_OnSuccess_DoesNotThrow()
    {
        var act = () => MetricRecorder.RecordOperationAsync(
            NewHistogram(), "op", () => Task.CompletedTask);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordOperationAsync_Void_OnException_RethrowsOriginalException()
    {
        var act = () => MetricRecorder.RecordOperationAsync(
            NewHistogram(), "op",
            () => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("boom");
    }

    [Fact]
    public async Task RecordOperationAsync_Void_OnTaskException_Rethrows()
    {
        var act = () => MetricRecorder.RecordOperationAsync(
            NewHistogram(), "op",
            () => Task.FromException(new ArgumentException("bad arg")));

        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("bad arg");
    }

    // ── typed overload ────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordOperationAsync_Typed_ReturnsResultFromFunc()
    {
        var result = await MetricRecorder.RecordOperationAsync(
            NewHistogram(), "op", () => Task.FromResult(42));

        result.Should().Be(42);
    }

    [Fact]
    public async Task RecordOperationAsync_Typed_OnSuccess_DoesNotThrow()
    {
        var act = () => MetricRecorder.RecordOperationAsync(
            NewHistogram(), "op", () => Task.FromResult("ok"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordOperationAsync_Typed_OnException_RethrowsOriginalException()
    {
        var act = () => MetricRecorder.RecordOperationAsync<string>(
            NewHistogram(), "op",
            () => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("boom");
    }

    [Fact]
    public async Task RecordOperationAsync_Typed_OnTaskException_Rethrows()
    {
        var act = () => MetricRecorder.RecordOperationAsync<string>(
            NewHistogram(), "op",
            () => Task.FromException<string>(new TimeoutException("timed out")));

        await act.Should().ThrowAsync<TimeoutException>()
                 .WithMessage("timed out");
    }

    [Fact]
    public async Task RecordOperationAsync_Typed_ExecutesFuncOnce()
    {
        var count = 0;
        await MetricRecorder.RecordOperationAsync(NewHistogram(), "op", () =>
        {
            count++;
            return Task.FromResult(count);
        });
        count.Should().Be(1);
    }
}
