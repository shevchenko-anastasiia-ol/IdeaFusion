using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Memory;
using Xunit;

namespace ServiceDefaults.Tests.Memory;

[Collection("CacheMetricsState")]
public class MemoryCacheServiceTests
{
    private readonly Mock<ILogger<MemoryCacheService>> _loggerMock = new();

    private MemoryCacheService CreateService(IMemoryCache? cache = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new MemoryCacheService(cache, _loggerMock.Object);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Get_WhenKeyExists_ReturnsValue()
    {
        var cache   = new MemoryCache(new MemoryCacheOptions());
        cache.Set("key", "stored-value");
        var service = CreateService(cache);

        var result = service.Get<string>("key");

        result.Should().Be("stored-value");
    }

    [Fact]
    public void Get_WhenKeyMissing_ReturnsDefault()
    {
        var service = CreateService();
        var result  = service.Get<string>("missing");
        result.Should().BeNull();
    }

    [Fact]
    public void Get_WhenKeyMissingForValueType_ReturnsDefault()
    {
        var service = CreateService();
        var result  = service.Get<int>("missing");
        result.Should().Be(0);
    }

    // ── Set ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Set_ThenGet_ReturnsStoredValue()
    {
        var service = CreateService();
        service.Set("key", "hello");
        service.Get<string>("key").Should().Be("hello");
    }

    [Fact]
    public void Set_WithCustomExpiration_StoresValue()
    {
        var service = CreateService();
        service.Set("key", 42, TimeSpan.FromMinutes(10));
        service.Get<int>("key").Should().Be(42);
    }

    [Fact]
    public void Set_UsesDefaultExpiration_WhenNotProvided()
    {
        var service = CreateService();
        service.Set("key", "value");
        service.Get<string>("key").Should().Be("value");
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var service = CreateService();
        service.Set("key", "first");
        service.Set("key", "second");
        service.Get<string>("key").Should().Be("second");
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_ExistingKey_ValueIsGone()
    {
        var service = CreateService();
        service.Set("key", "value");
        service.Remove("key");
        service.Get<string>("key").Should().BeNull();
    }

    [Fact]
    public void Remove_NullKey_DoesNotThrow()
    {
        var service = CreateService();
        var act     = () => service.Remove(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_EmptyKey_DoesNotThrow()
    {
        var service = CreateService();
        var act     = () => service.Remove("");
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_WhitespaceKey_DoesNotThrow()
    {
        var service = CreateService();
        var act     = () => service.Remove("   ");
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_MissingKey_DoesNotThrow()
    {
        var service = CreateService();
        var act     = () => service.Remove("nonexistent");
        act.Should().NotThrow();
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("a", 1);
        cache.Set("b", 2);
        var service = CreateService(cache);

        service.Clear();

        service.Get<int>("a").Should().Be(0);
        service.Get<int>("b").Should().Be(0);
    }

    [Fact]
    public void Clear_DoesNotThrow_WhenCacheIsEmpty()
    {
        var service = CreateService();
        var act     = () => service.Clear();
        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_WithMockedIMemoryCache_DoesNotThrow()
    {
        // When _cache is not a MemoryCache instance, Clear is a no-op
        var cacheMock = new Mock<IMemoryCache>();
        var service   = new MemoryCacheService(cacheMock.Object, _loggerMock.Object);

        var act = () => service.Clear();
        act.Should().NotThrow();
    }
}
