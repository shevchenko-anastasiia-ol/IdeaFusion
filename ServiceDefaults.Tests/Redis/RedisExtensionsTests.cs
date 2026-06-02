using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Redis;
using StackExchange.Redis;
using Xunit;

namespace ServiceDefaults.Tests.Redis;

public class RedisExtensionsTests
{
    // ── AddRedisCache ─────────────────────────────────────────────────────────

    [Fact]
    public void AddRedisCache_WithConnectionString_RegistersIConnectionMultiplexerAsSingleton()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379");

        services.AddRedisCache(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConnectionMultiplexer) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddRedisCache_WithConnectionString_RegistersIRedisCacheServiceAsSingleton()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379");

        services.AddRedisCache(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IRedisCacheService) &&
            sd.ImplementationType == typeof(RedisCacheService) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddRedisCache_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379");

        var result = services.AddRedisCache(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddRedisCache_NoConnectionString_ThrowsWhenResolvingMultiplexer()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();
        services.AddRedisCache(config);

        var sp  = services.BuildServiceProvider();
        var act = () => sp.GetRequiredService<IConnectionMultiplexer>();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Redis*");
    }

    [Fact]
    public void AddRedisCache_ReadsConnectionStringFromRedisKey()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379", "redis");

        services.AddRedisCache(config);

        services.Should().Contain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    [Fact]
    public void AddRedisCache_ReadsConnectionStringFromCapitalRedisKey()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379", "Redis");

        services.AddRedisCache(config);

        services.Should().Contain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    // ── AddDistributedRedisCache ──────────────────────────────────────────────

    [Fact]
    public void AddDistributedRedisCache_WithConnectionString_RegistersIDistributedCache()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379");

        services.AddDistributedRedisCache(config, "MyService");

        services.Should().Contain(sd => sd.ServiceType == typeof(IDistributedCache));
    }

    [Fact]
    public void AddDistributedRedisCache_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("localhost:6379");

        var result = services.AddDistributedRedisCache(config, "MyService");

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddDistributedRedisCache_MissingConnectionString_ThrowsImmediately()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var act = () => services.AddDistributedRedisCache(config, "MyService");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Redis*");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(string connectionString, string key = "redis")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{key}"] = connectionString
            })
            .Build();
    }
}
