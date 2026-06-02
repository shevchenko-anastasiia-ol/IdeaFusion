using FluentAssertions;
using ServiceDefaults.Metrics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Xunit;

namespace ServiceDefaults.Tests.Metrics;

[Collection("CacheMetricsState")]
public class CacheMetricsTests
{
    // ── counters not null ─────────────────────────────────────────────────────

    [Fact] public void MemoryCacheHits_IsNotNull()    => CacheMetrics.MemoryCacheHits.Should().NotBeNull();
    [Fact] public void MemoryCacheMisses_IsNotNull()  => CacheMetrics.MemoryCacheMisses.Should().NotBeNull();
    [Fact] public void RedisCacheHits_IsNotNull()     => CacheMetrics.RedisCacheHits.Should().NotBeNull();
    [Fact] public void RedisCacheMisses_IsNotNull()   => CacheMetrics.RedisCacheMisses.Should().NotBeNull();
    [Fact] public void CacheEvictions_IsNotNull()     => CacheMetrics.CacheEvictions.Should().NotBeNull();
    [Fact] public void CacheInvalidations_IsNotNull() => CacheMetrics.CacheInvalidations.Should().NotBeNull();

    // ── histogram not null ────────────────────────────────────────────────────

    [Fact] public void CacheLatency_IsNotNull() => CacheMetrics.CacheLatency.Should().NotBeNull();

    // ── observable gauge not null ─────────────────────────────────────────────

    [Fact] public void MemoryCacheSize_IsNotNull() => CacheMetrics.MemoryCacheSize.Should().NotBeNull();

    // ── instrument names ──────────────────────────────────────────────────────

    [Fact] public void MemoryCacheHits_Name()    => CacheMetrics.MemoryCacheHits.Name.Should().Be("cache_memory_hit_total");
    [Fact] public void MemoryCacheMisses_Name()  => CacheMetrics.MemoryCacheMisses.Name.Should().Be("cache_memory_miss_total");
    [Fact] public void RedisCacheHits_Name()     => CacheMetrics.RedisCacheHits.Name.Should().Be("cache_redis_hit_total");
    [Fact] public void RedisCacheMisses_Name()   => CacheMetrics.RedisCacheMisses.Name.Should().Be("cache_redis_miss_total");
    [Fact] public void CacheLatency_Name()       => CacheMetrics.CacheLatency.Name.Should().Be("cache_latency_seconds");
    [Fact] public void CacheEvictions_Name()     => CacheMetrics.CacheEvictions.Name.Should().Be("cache_evictions_total");
    [Fact] public void CacheInvalidations_Name() => CacheMetrics.CacheInvalidations.Name.Should().Be("cache_invalidation_total");
    [Fact] public void MemoryCacheSize_Name()    => CacheMetrics.MemoryCacheSize.Name.Should().Be("cache_memory_size_items");

    // ── UpdateMemoryCacheSize ─────────────────────────────────────────────────

    [Fact]
    public void UpdateMemoryCacheSize_UpdatesBackingField()
    {
        CacheMetrics.UpdateMemoryCacheSize(99);

        var field = typeof(CacheMetrics)
            .GetField("_memoryCacheSize", BindingFlags.NonPublic | BindingFlags.Static)!;
        ((long)field.GetValue(null)!).Should().Be(99);

        CacheMetrics.UpdateMemoryCacheSize(0); // restore
    }

    [Fact]
    public void UpdateMemoryCacheSize_DoesNotThrow()
    {
        var act = () => CacheMetrics.UpdateMemoryCacheSize(0);
        act.Should().NotThrow();
    }

    // ── Add / Record do not throw ─────────────────────────────────────────────

    [Fact]
    public void MemoryCacheHits_Add_DoesNotThrow()
    {
        var act = () => CacheMetrics.MemoryCacheHits.Add(1);
        act.Should().NotThrow();
    }

    [Fact]
    public void CacheLatency_Record_DoesNotThrow()
    {
        var act = () => CacheMetrics.CacheLatency.Record(0.001);
        act.Should().NotThrow();
    }

    // ── ObservableGauge callback ──────────────────────────────────────────────

    [Fact]
    public void MemoryCacheSize_ObservableGauge_ReportsCurrentValue()
    {
        CacheMetrics.UpdateMemoryCacheSize(55);

        long observed = -1;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "cache_memory_size_items")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "cache_memory_size_items")
                observed = measurement;
        });
        listener.Start();
        listener.RecordObservableInstruments();

        observed.Should().Be(55);
        CacheMetrics.UpdateMemoryCacheSize(0);
    }
}
