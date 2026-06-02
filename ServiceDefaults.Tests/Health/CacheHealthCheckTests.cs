using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ServiceDefaults.Health;
using StackExchange.Redis;
using Xunit;

namespace ServiceDefaults.Tests.Health;

public class CacheHealthCheckTests
{
    private static IMemoryCache CreateTrackingCache() =>
        new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });

    private static HealthCheckContext MakeContext(IHealthCheck check) => new()
    {
        Registration = new HealthCheckRegistration("test", check, null, null)
    };

    // ── healthy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_WhenRedisResponds_ReturnsHealthy()
    {
        var (check, _) = BuildCheck(pingMs: 1);
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisResponds_DataContainsConnectedStatus()
    {
        var (check, _) = BuildCheck(pingMs: 5);
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Data.Should().ContainKey("L2_Redis_Status");
        result.Data["L2_Redis_Status"].Should().Be("Connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisResponds_DataContainsPingMs()
    {
        var (check, _) = BuildCheck(pingMs: 10);
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Data.Should().ContainKey("L2_Redis_PingMs");
    }

    // ── degraded path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_WhenRedisConnectionFails_ReturnsDegraded()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ThrowsAsync(new RedisConnectionException(
                  ConnectionFailureType.UnableToConnect, "Redis unavailable"));

        var multiplexerMock = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object);

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisConnectionFails_DataContainsDisconnectedStatus()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ThrowsAsync(new RedisConnectionException(
                  ConnectionFailureType.UnableToConnect, "no connection"));

        var multiplexerMock = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object);

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Data.Should().ContainKey("L2_Redis_Status");
        result.Data["L2_Redis_Status"].Should().Be("Disconnected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisThrowsGenericException_ReturnsDegraded()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ThrowsAsync(new Exception("unexpected error"));

        var multiplexerMock = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object);

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    // ── null memory cache stats ───────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_WithNonTrackingMemoryCache_SkipsMemoryCacheStats()
    {
        // MemoryCache without TrackStatistics returns null from GetCurrentStatistics()
        var nonTrackingCache = new MemoryCache(new MemoryCacheOptions());
        var (check, _) = BuildCheckWithCache(nonTrackingCache, pingMs: 1);

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().NotContainKey("L1_MemoryCache_Hits");
    }

    // ── memory cache stats ────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_TrackingCacheWithHits_DataContainsCacheStats()
    {
        var cache = CreateTrackingCache();
        cache.Set("key", "value");
        _ = cache.TryGetValue("key", out _);  // record a hit

        var (check, _) = BuildCheckWithCache(cache, pingMs: 1);
        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Data.Should().ContainKey("L1_MemoryCache_Hits");
        result.Data.Should().ContainKey("L1_MemoryCache_Misses");
        result.Data.Should().ContainKey("L1_MemoryCache_Entries");
    }

    // ── OperationCanceledException ────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_WhenPingThrowsOperationCanceledException_ReturnsDegraded()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ThrowsAsync(new OperationCanceledException());

        var multiplexerMock = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object, TimeSpan.FromSeconds(1));

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["L2_Redis_Status"].Should().Be("Timeout");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingThrowsOperationCanceledException_DataContainsTimeoutError()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ThrowsAsync(new OperationCanceledException());

        var multiplexerMock = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object, TimeSpan.FromSeconds(2));

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Data.Should().ContainKey("L2_Redis_Error");
        result.Data["L2_Redis_Error"].ToString().Should().Contain("2");
    }

    // ── constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithDefaultTimeout_DoesNotThrow()
    {
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        var act = () => new CacheHealthCheck(CreateTrackingCache(), multiplexerMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithCustomTimeout_DoesNotThrow()
    {
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        var act = () => new CacheHealthCheck(
            CreateTrackingCache(), multiplexerMock.Object, TimeSpan.FromSeconds(5));
        act.Should().NotThrow();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static (CacheHealthCheck check, Mock<IConnectionMultiplexer> mux) BuildCheck(int pingMs)
        => BuildCheckWithCache(CreateTrackingCache(), pingMs);

    private static (CacheHealthCheck check, Mock<IConnectionMultiplexer> mux) BuildCheckWithCache(
        IMemoryCache cache, int pingMs)
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
              .ReturnsAsync(TimeSpan.FromMilliseconds(pingMs));

        var mux = BuildMultiplexer(dbMock.Object);
        var check = new CacheHealthCheck(cache, mux.Object);
        return (check, mux);
    }

    private static Mock<IConnectionMultiplexer> BuildMultiplexer(IDatabase db)
    {
        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
           .Returns(db);
        mux.Setup(m => m.GetEndPoints(It.IsAny<bool>()))
           .Returns(Array.Empty<System.Net.EndPoint>());
        return mux;
    }
}
