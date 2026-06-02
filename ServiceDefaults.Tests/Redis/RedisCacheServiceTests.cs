using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Redis;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace ServiceDefaults.Tests.Redis;

public class RedisCacheServiceTests
{
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock = new();
    private readonly Mock<IDatabase>                  _dbMock     = new();
    private readonly Mock<IConnectionMultiplexer>     _muxMock    = new();

    public RedisCacheServiceTests()
    {
        _muxMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(_dbMock.Object);
    }

    private RedisCacheService CreateService() =>
        new(_muxMock.Object, _loggerMock.Object);

    // ── constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullMultiplexer_ThrowsArgumentNullException()
    {
        var act = () => new RedisCacheService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("multiplexer");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RedisCacheService(_muxMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GetMultiplexer_ReturnsInjectedMultiplexer()
    {
        var service = CreateService();
        service.GetMultiplexer().Should().BeSameAs(_muxMock.Object);
    }

    // ── GetDataAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDataAsync_EmptyKey_ReturnsDefault()
    {
        var result = await CreateService().GetDataAsync<string>("");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataAsync_WhitespaceKey_ReturnsDefault()
    {
        var result = await CreateService().GetDataAsync<string>("   ");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataAsync_NullKey_ReturnsDefault()
    {
        var result = await CreateService().GetDataAsync<string>(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataAsync_KeyNotFound_ReturnsDefault()
    {
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);

        var result = await CreateService().GetDataAsync<string>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataAsync_KeyFound_ReturnsDeserializedValue()
    {
        var json = JsonSerializer.Serialize("hello");
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)json);
        _dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((TimeSpan?)null);

        var result = await CreateService().GetDataAsync<string>("key");

        result.Should().Be("hello");
    }

    [Fact]
    public async Task GetDataAsync_KeyFoundWithNonNullTtl_LogsTtl()
    {
        var json = JsonSerializer.Serialize("hello");
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)json);
        _dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(TimeSpan.FromMinutes(5));

        var result = await CreateService().GetDataAsync<string>("key");

        result.Should().Be("hello");
    }

    [Fact]
    public async Task GetDataAsync_KeyFound_DeserializesComplexType()
    {
        var obj  = new TestDto { Id = 7, Name = "test" };
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)json);
        _dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((TimeSpan?)null);

        var result = await CreateService().GetDataAsync<TestDto>("key");

        result!.Id.Should().Be(7);
        result.Name.Should().Be("test");
    }

    // ── SetDataAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SetDataAsync_EmptyKey_DoesNotCallStringSet()
    {
        await CreateService().SetDataAsync("", "value");
        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task SetDataAsync_NullData_DoesNotCallStringSet()
    {
        await CreateService().SetDataAsync<string?>("key", null);
        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task SetDataAsync_ValidKeyAndData_CallsStringSet()
    {
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().SetDataAsync("key", "value");

        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetDataAsync_UsesCustomExpiration()
    {
        var expiration = TimeSpan.FromMinutes(10);
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            expiration, It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().SetDataAsync("key", "value", expiration);

        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            expiration, It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── RemoveDataAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveDataAsync_EmptyKey_DoesNotCallKeyDelete()
    {
        await CreateService().RemoveDataAsync("");
        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveDataAsync_ValidKey_CallsKeyDelete()
    {
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().RemoveDataAsync("key");

        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── AddToSetAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddToSetAsync_EmptySetKey_DoesNotCallSetAdd()
    {
        await CreateService().AddToSetAsync("", "value");
        _dbMock.Verify(d => d.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task AddToSetAsync_EmptyValue_DoesNotCallSetAdd()
    {
        await CreateService().AddToSetAsync("set", "");
        _dbMock.Verify(d => d.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task AddToSetAsync_ValidArgs_CallsSetAdd()
    {
        _dbMock.Setup(d => d.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().AddToSetAsync("myset", "member");

        _dbMock.Verify(d => d.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── GetSetMembersAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSetMembersAsync_EmptyKey_ReturnsEmpty()
    {
        var result = await CreateService().GetSetMembersAsync("");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSetMembersAsync_ValidKey_ReturnsMembers()
    {
        _dbMock.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue[] { "a", "b" });

        var result = await CreateService().GetSetMembersAsync("myset");

        result.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    // ── ClearSetAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearSetAsync_EmptyKey_DoesNotCallKeyDelete()
    {
        await CreateService().ClearSetAsync("");
        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task ClearSetAsync_ValidKey_CallsKeyDelete()
    {
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().ClearSetAsync("myset");

        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── retry policy lambdas ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDataAsync_RedisExceptionThenSuccess_RetriesAndReturnsValue()
    {
        var attempts = 0;
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .Returns(() =>
               {
                   if (++attempts == 1)
                       return Task.FromException<RedisValue>(new RedisException("transient"));
                   return Task.FromResult((RedisValue)JsonSerializer.Serialize("hello"));
               });
        _dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((TimeSpan?)null);

        var result = await CreateService().GetDataAsync<string>("key");

        result.Should().Be("hello");
        attempts.Should().Be(2);
    }

    // ── RemoveByPatternAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RemoveByPatternAsync_EmptyPattern_DoesNotCallGetEndPoints()
    {
        await CreateService().RemoveByPatternAsync("");
        _muxMock.Verify(m => m.GetEndPoints(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhitespacePattern_DoesNotCallGetEndPoints()
    {
        await CreateService().RemoveByPatternAsync("   ");
        _muxMock.Verify(m => m.GetEndPoints(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPatternAsync_ValidPattern_DeletesMatchingKeys()
    {
        var endpoint   = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379);
        var serverMock = new Mock<IServer>();

        _muxMock.Setup(m => m.GetEndPoints(It.IsAny<bool>()))
                .Returns(new System.Net.EndPoint[] { endpoint });
        _muxMock.Setup(m => m.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object?>()))
                .Returns(serverMock.Object);
        serverMock.Setup(s => s.KeysAsync(
                It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(),
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(AsyncKeys("cache:key1", "cache:key2"));
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        await CreateService().RemoveByPatternAsync("cache:*");

        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RemoveByPatternAsync_ValidPattern_NoMatchingKeys_DoesNotCallKeyDelete()
    {
        var endpoint   = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379);
        var serverMock = new Mock<IServer>();

        _muxMock.Setup(m => m.GetEndPoints(It.IsAny<bool>()))
                .Returns(new System.Net.EndPoint[] { endpoint });
        _muxMock.Setup(m => m.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object?>()))
                .Returns(serverMock.Object);
        serverMock.Setup(s => s.KeysAsync(
                It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(),
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(AsyncKeys());

        await CreateService().RemoveByPatternAsync("nokeys:*");

        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenExceptionThrown_Rethrows()
    {
        var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379);
        _muxMock.Setup(m => m.GetEndPoints(It.IsAny<bool>()))
                .Returns(new System.Net.EndPoint[] { endpoint });
        _muxMock.Setup(m => m.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object?>()))
                .Throws(new RedisException("server unavailable"));

        var act = () => CreateService().RemoveByPatternAsync("key:*");

        await act.Should().ThrowAsync<RedisException>();
    }

    private static async IAsyncEnumerable<RedisKey> AsyncKeys(params string[] keys)
    {
        foreach (var key in keys)
        {
            await Task.Yield();
            yield return (RedisKey)key;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private class TestDto
    {
        public int    Id   { get; set; }
        public string Name { get; set; } = "";
    }
}
