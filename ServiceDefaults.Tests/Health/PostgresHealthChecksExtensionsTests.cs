using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ServiceDefaults.Health;
using Xunit;

namespace ServiceDefaults.Tests.Health;

public class PostgresHealthChecksExtensionsTests
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

    private static IConfiguration BuildConfig(string? connectionString, string key = "mydb")
    {
        var pairs = connectionString is not null
            ? new Dictionary<string, string?> { [$"ConnectionStrings:{key}"] = connectionString }
            : new Dictionary<string, string?>();
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void AddPostgresHealthCheck_ValidConnectionString_ReturnsBuilder()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("Host=localhost;Database=test");

        var result = builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        result.Should().BeSameAs(builder.Object);
    }

    [Fact]
    public void AddPostgresHealthCheck_RegistersCheckWithCorrectName()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("Host=localhost;Database=test");

        builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        registrations.Should().ContainSingle(r => r.Name == "myservice-postgresql");
    }

    [Fact]
    public void AddPostgresHealthCheck_RegistersWithUnhealthyFailureStatus()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("Host=localhost;Database=test");

        builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        registrations.Single().FailureStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void AddPostgresHealthCheck_RegistersWithExpectedTags()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("Host=localhost;Database=test");

        builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        registrations.Single().Tags.Should().Contain(new[] { "database", "postgresql", "ready", "live" });
    }

    [Fact]
    public void AddPostgresHealthCheck_ServiceNameIsUsedInCheckName()
    {
        var (builder, registrations) = CreateBuilder();
        var config = BuildConfig("Host=localhost;Database=test");

        builder.Object.AddPostgresHealthCheck(config, "mydb", "orders");

        registrations.Single().Name.Should().Be("orders-postgresql");
    }

    // ── error path ────────────────────────────────────────────────────────────

    [Fact]
    public void AddPostgresHealthCheck_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig(null);

        var act = () => builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*mydb*");
    }

    [Fact]
    public void AddPostgresHealthCheck_EmptyConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("");

        var act = () => builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPostgresHealthCheck_WhitespaceConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config       = BuildConfig("   ");

        var act = () => builder.Object.AddPostgresHealthCheck(config, "mydb", "myservice");

        act.Should().Throw<InvalidOperationException>();
    }
}
