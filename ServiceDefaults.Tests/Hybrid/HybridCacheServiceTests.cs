using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Hybrid;
using ServiceDefaults.Memory;
using ServiceDefaults.Redis;
using StackExchange.Redis;
using Xunit;

namespace ServiceDefaults.Tests.Hybrid;

public class HybridCacheServiceTests
{
    private readonly Mock<IMemoryCacheService>            _memoryCacheMock  = new();
    private readonly Mock<IRedisCacheService>             _redisCacheMock   = new();
    private readonly Mock<ISubscriber>                    _subscriberMock   = new();
    private readonly Mock<IConnectionMultiplexer>         _multiplexerMock  = new();
    private readonly Mock<ILogger<HybridCacheService>>    _loggerMock       = new();

    private Action<RedisChannel, RedisValue>? _capturedSubscriptionHandler;

    public HybridCacheServiceTests()
    {
        _multiplexerMock.Setup(m => m.GetSubscriber(It.IsAny<object?>()))
                        .Returns(_subscriberMock.Object);

        _subscriberMock
            .Setup(s => s.Subscribe(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>(
                (_, handler, _) => _capturedSubscriptionHandler = handler);

        _subscriberMock
            .Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);
    }

    private HybridCacheService CreateService() =>
        new(_memoryCacheMock.Object, _redisCacheMock.Object,
            _multiplexerMock.Object, _loggerMock.Object);

    // ── GetOrSetAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrSetAsync_L1Hit_ReturnsL1Value()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns("l1-value");

        var result = await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>("db"));

        result.Should().Be("l1-value");
    }

    [Fact]
    public async Task GetOrSetAsync_L1Hit_DoesNotQueryRedis()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns("l1-value");

        await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>("db"));

        _redisCacheMock.Verify(r => r.GetDataAsync<string>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_L1Miss_L2Hit_ReturnsL2Value()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns((string?)null);
        _redisCacheMock.Setup(r => r.GetDataAsync<string>("key")).ReturnsAsync("l2-value");

        var result = await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>("db"));

        result.Should().Be("l2-value");
    }

    [Fact]
    public async Task GetOrSetAsync_L1Miss_L2Hit_PopulatesL1Cache()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns((string?)null);
        _redisCacheMock.Setup(r => r.GetDataAsync<string>("key")).ReturnsAsync("l2-value");

        await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>("db"));

        _memoryCacheMock.Verify(m => m.Set(
            "key", "l2-value", It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_BothMiss_CallsFactory()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns((string?)null);
        _redisCacheMock.Setup(r => r.GetDataAsync<string>("key")).ReturnsAsync((string?)null);
        var factoryCalled = false;

        await CreateService().GetOrSetAsync("key", () =>
        {
            factoryCalled = true;
            return Task.FromResult<string?>("db-value");
        });

        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrSetAsync_BothMiss_FactoryReturnsValue_CachesInBothLayers()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns((string?)null);
        _redisCacheMock.Setup(r => r.GetDataAsync<string>("key")).ReturnsAsync((string?)null);
        _redisCacheMock.Setup(r => r.SetDataAsync("key", "db-value", It.IsAny<TimeSpan?>()))
                       .Returns(Task.CompletedTask);

        await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>("db-value"));

        _redisCacheMock.Verify(r => r.SetDataAsync("key", "db-value", It.IsAny<TimeSpan?>()), Times.Once);
        _memoryCacheMock.Verify(m => m.Set("key", "db-value", It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_BothMiss_FactoryReturnsNull_DoesNotCache()
    {
        _memoryCacheMock.Setup(m => m.Get<string>("key")).Returns((string?)null);
        _redisCacheMock.Setup(r => r.GetDataAsync<string>("key")).ReturnsAsync((string?)null);

        await CreateService().GetOrSetAsync("key", () => Task.FromResult<string?>(null));

        _redisCacheMock.Verify(r => r.SetDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
        _memoryCacheMock.Verify(m => m.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    // ── SetAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_SetsValueInL1Cache()
    {
        _redisCacheMock.Setup(r => r.SetDataAsync("key", "value", It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);

        await CreateService().SetAsync("key", "value");

        _memoryCacheMock.Verify(m => m.Set("key", "value", It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_SetsValueInL2Cache()
    {
        _redisCacheMock.Setup(r => r.SetDataAsync("key", "value", It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);

        await CreateService().SetAsync("key", "value");

        _redisCacheMock.Verify(r => r.SetDataAsync("key", "value", It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_PublishesInvalidation()
    {
        _redisCacheMock.Setup(r => r.SetDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                       .Returns(Task.CompletedTask);

        await CreateService().SetAsync("key", "value");

        _subscriberMock.Verify(s => s.PublishAsync(
            It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── RemoveAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_RemovesFromL1Cache()
    {
        _redisCacheMock.Setup(r => r.RemoveDataAsync("key")).Returns(Task.CompletedTask);

        await CreateService().RemoveAsync("key");

        _memoryCacheMock.Verify(m => m.Remove("key"), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromL2Cache()
    {
        _redisCacheMock.Setup(r => r.RemoveDataAsync("key")).Returns(Task.CompletedTask);

        await CreateService().RemoveAsync("key");

        _redisCacheMock.Verify(r => r.RemoveDataAsync("key"), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_PublishesInvalidation()
    {
        _redisCacheMock.Setup(r => r.RemoveDataAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateService().RemoveAsync("key");

        _subscriberMock.Verify(s => s.PublishAsync(
            It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── RemoveByPatternAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RemoveByPatternAsync_DelegatesToRedisCache()
    {
        _redisCacheMock.Setup(r => r.RemoveByPatternAsync("*test*")).Returns(Task.CompletedTask);

        await CreateService().RemoveByPatternAsync("*test*");

        _redisCacheMock.Verify(r => r.RemoveByPatternAsync("*test*"), Times.Once);
    }

    [Fact]
    public async Task RemoveByPatternAsync_PublishesClearAllSignal()
    {
        _redisCacheMock.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateService().RemoveByPatternAsync("*test*");

        _subscriberMock.Verify(s => s.PublishAsync(
            It.IsAny<RedisChannel>(),
            It.Is<RedisValue>(v => v.ToString() == "__CLEAR_ALL__"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── invalidation channel callback ─────────────────────────────────────────

    [Fact]
    public void InvalidationCallback_NormalKey_RemovesFromMemoryCache()
    {
        CreateService();
        _capturedSubscriptionHandler.Should().NotBeNull();

        _capturedSubscriptionHandler!(RedisChannel.Literal("cache-invalidation-channel"), "my-key");

        _memoryCacheMock.Verify(m => m.Remove("my-key"), Times.Once);
    }

    [Fact]
    public void InvalidationCallback_ClearAllMessage_ClearsMemoryCache()
    {
        CreateService();
        _capturedSubscriptionHandler!(RedisChannel.Literal("cache-invalidation-channel"), "__CLEAR_ALL__");

        _memoryCacheMock.Verify(m => m.Clear(), Times.Once);
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromInvalidationChannel()
    {
        var service = CreateService();
        service.Dispose();

        _subscriberMock.Verify(s => s.Unsubscribe(
            It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotUnsubscribeTwice()
    {
        var service = CreateService();
        service.Dispose();
        service.Dispose();

        _subscriberMock.Verify(s => s.Unsubscribe(
            It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void HybridCacheService_ImplementsIHybridCacheService()
    {
        typeof(HybridCacheService).IsAssignableTo(typeof(IHybridCacheService)).Should().BeTrue();
    }

    [Fact]
    public void HybridCacheService_ImplementsIDisposable()
    {
        typeof(HybridCacheService).IsAssignableTo(typeof(IDisposable)).Should().BeTrue();
    }
}
