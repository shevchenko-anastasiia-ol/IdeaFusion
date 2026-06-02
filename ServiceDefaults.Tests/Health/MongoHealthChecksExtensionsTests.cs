using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Moq;
using ServiceDefaults.Health;
using Xunit;

namespace ServiceDefaults.Tests.Health;

public class MongoHealthChecksExtensionsTests
{
    private static (Mock<IHealthChecksBuilder> builder, ServiceCollection services) CreateBuilder()
    {
        var services = new ServiceCollection();
        var builder  = new Mock<IHealthChecksBuilder>();
        builder.Setup(b => b.Services).Returns(services);
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Returns(builder.Object);
        return (builder, services);
    }

    private static IConfiguration BuildConfig(string value, string key = "mongo")
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{key}"] = value })
            .Build();

    // ── error paths ───────────────────────────────────────────────────────────

    [Fact]
    public void AddMongoHealthCheck_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config = new ConfigurationBuilder().Build();

        var act = () => builder.Object.AddMongoHealthCheck(config, "missing", "svc");

        act.Should().Throw<InvalidOperationException>().WithMessage("*missing*");
    }

    [Fact]
    public void AddMongoHealthCheck_EmptyConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("", "mongo");

        var act = () => builder.Object.AddMongoHealthCheck(config, "mongo", "svc");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddMongoHealthCheck_WhitespaceConnectionString_ThrowsInvalidOperationException()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("   ", "mongo");

        var act = () => builder.Object.AddMongoHealthCheck(config, "mongo", "svc");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void AddMongoHealthCheck_ValidConnectionString_DoesNotThrow()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        var act = () => builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMongoHealthCheck_ValidConnectionString_RegistersIMongoClientAsSingleton()
    {
        var (builder, services) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMongoClient) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongoHealthCheck_ValidConnectionString_AddsRegistrationWithCorrectName()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        HealthCheckRegistration? captured = null;
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => captured = r)
               .Returns(builder.Object);

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("myservice-mongodb");
    }

    [Fact]
    public void AddMongoHealthCheck_ValidConnectionString_UsesUnhealthyFailureStatus()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        HealthCheckRegistration? captured = null;
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => captured = r)
               .Returns(builder.Object);

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        captured!.FailureStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void AddMongoHealthCheck_ValidConnectionString_IncludesMongoDbTag()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        HealthCheckRegistration? captured = null;
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => captured = r)
               .Returns(builder.Object);

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        captured!.Tags.Should().Contain("mongodb");
    }

    [Fact]
    public void AddMongoHealthCheck_WithExplicitDatabaseName_DoesNotThrow()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        var act = () => builder.Object.AddMongoHealthCheck(config, "mongo", "myservice", "mydatabase");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMongoHealthCheck_WithCustomTimeoutSeconds_DoesNotThrow()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        var act = () => builder.Object.AddMongoHealthCheck(config, "mongo", "myservice", timeoutSeconds: 10);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMongoHealthCheck_ReturnsBuilderInstance()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        var result = builder.Object.AddMongoHealthCheck(config, "mongo", "myservice");

        result.Should().BeSameAs(builder.Object);
    }

    // ── databaseNameFactory lambda ─────────────────────────────────────────────

    [Fact]
    public void AddMongoHealthCheck_NullDatabaseName_FactoryReturnsAdmin()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        HealthCheckRegistration? captured = null;
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => captured = r)
               .Returns(builder.Object);

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice", databaseName: null);

        // Invoke the health check factory to trigger databaseNameFactory(sp) => null ?? "admin"
        var mongoMock = new Mock<IMongoClient>();
        var spMock    = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IMongoClient))).Returns(mongoMock.Object);

        var healthCheck = captured!.Factory(spMock.Object);
        healthCheck.Should().NotBeNull();
    }

    [Fact]
    public void AddMongoHealthCheck_ExplicitDatabaseName_FactoryUsesProvidedName()
    {
        var (builder, _) = CreateBuilder();
        var config = BuildConfig("mongodb://localhost:27017");

        HealthCheckRegistration? captured = null;
        builder.Setup(b => b.Add(It.IsAny<HealthCheckRegistration>()))
               .Callback<HealthCheckRegistration>(r => captured = r)
               .Returns(builder.Object);

        builder.Object.AddMongoHealthCheck(config, "mongo", "myservice", databaseName: "mydb");

        var mongoMock = new Mock<IMongoClient>();
        var spMock    = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IMongoClient))).Returns(mongoMock.Object);

        var healthCheck = captured!.Factory(spMock.Object);
        healthCheck.Should().NotBeNull();
    }
}
