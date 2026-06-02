using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ServiceDefaults.Health;
using Xunit;

namespace ServiceDefaults.Tests.Health;

public class RedisHealthChecksExtensionsTests
{
    private static (Mock<IHealthChecksBuilder> builder, List<HealthCheckRegistration> registrations)
        CreateBuilder()
    {
        var registrations = new List<HealthCheckRegistration>();
        var builder       = new Mock<IHealthChecksBuilder>();
        builder.Setup(b => b.Services).Returns(new ServiceCollection());
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => registrations.Add(r))
               .Returns(builder.Object);
        return (builder, registrations);
    }

    private static IConfiguration BuildConfig(string? connectionString, string key = "redis")
    {
        var pairs = connectionString is not null
            ? new Dictionary<string, string?> { [$"ConnectionStrings:{key}"] = connectionString }
            : new Dictionary<string, string?>();
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void AddRedisHealthCheck_ValidConnectionString_ReturnsBuilder()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("localhost:6379");

        var result = builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        result.Should().BeSameAs(builder.Object);
    }

    [Fact]
    public void AddRedisHealthCheck_RegistersCheckWithCorrectName()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("localhost:6379");

        builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        registrations.Should().ContainSingle(r => r.Name == "myservice-redis");
    }

    [Fact]
    public void AddRedisHealthCheck_RegistersWithDegradedFailureStatus()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("localhost:6379");

        builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        registrations.Single().FailureStatus.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void AddRedisHealthCheck_RegistersWithExpectedTags()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("localhost:6379");

        builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        registrations.Single().Tags.Should().Contain(new[] { "cache", "redis", "ready" });
    }

    [Fact]
    public void AddRedisHealthCheck_ServiceNameIsUsedInCheckName()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("localhost:6379");

        builder.Object.AddRedisHealthCheck(config, "redis", "cache-service");

        registrations.Single().Name.Should().Be("cache-service-redis");
    }

    // ── error path ────────────────────────────────────────────────────────────

    [Fact]
    public void AddRedisHealthCheck_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig(null);

        var act = () => builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*redis*");
    }

    [Fact]
    public void AddRedisHealthCheck_EmptyConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("");

        var act = () => builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddRedisHealthCheck_WhitespaceConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("   ");

        var act = () => builder.Object.AddRedisHealthCheck(config, "redis", "myservice");

        act.Should().Throw<InvalidOperationException>();
    }
}
