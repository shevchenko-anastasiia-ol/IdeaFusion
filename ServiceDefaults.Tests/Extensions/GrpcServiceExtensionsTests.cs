using FluentAssertions;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using ServiceDefaults.Extensions;
using ServiceDefaults.Grpc.Interceptors;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class GrpcServiceExtensionsTests
{
    // ── AddGrpcWithObservability ───────────────────────────────────────────────

    [Fact]
    public void AddGrpcWithObservability_NullEnv_RegistersGrpcLoggingInterceptorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddGrpcWithObservability(env: null);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(GrpcLoggingInterceptor) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGrpcWithObservability_NullEnv_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddGrpcWithObservability(env: null);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddGrpcWithObservability_DevEnvironment_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var devEnv   = CreateEnv("Development");

        var act = () => services.AddGrpcWithObservability(devEnv);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddGrpcWithObservability_DevEnvironment_RegistersGrpcLoggingInterceptor()
    {
        var services = new ServiceCollection();
        var devEnv   = CreateEnv("Development");

        services.AddGrpcWithObservability(devEnv);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(GrpcLoggingInterceptor) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGrpcWithObservability_NonDevEnvironment_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var prodEnv  = CreateEnv("Production");

        var act = () => services.AddGrpcWithObservability(prodEnv);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddGrpcWithObservability_NonDevEnvironment_RegistersGrpcLoggingInterceptor()
    {
        var services = new ServiceCollection();
        var prodEnv  = CreateEnv("Production");

        services.AddGrpcWithObservability(prodEnv);

        services.Should().Contain(sd => sd.ServiceType == typeof(GrpcLoggingInterceptor));
    }

    [Fact]
    public void AddGrpcWithObservability_NullEnv_OptionsLambdaIsCovered()
    {
        // Explicitly resolving GrpcServiceOptions forces the AddGrpc lambda to run with env=null,
        // covering the env?.IsDevelopment() null-conditional branch inside the lambda.
        var services = new ServiceCollection();
        services.AddGrpcWithObservability(env: null);

        using var sp   = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<GrpcServiceOptions>>();

        opts.Value.EnableDetailedErrors.Should().BeFalse();
    }

    [Fact]
    public void AddGrpcWithObservability_ProductionEnv_BuildingWebApp_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Services.AddGrpcWithObservability(CreateEnv("Production"));

        var act = () => { using var app = builder.Build(); };

        act.Should().NotThrow();
    }

    // ── MapGrpcServicesWithReflection ─────────────────────────────────────────

    [Fact]
    public void MapGrpcServicesWithReflection_NonDevEnvironment_ReturnsSameApp()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Production" });
        var app = builder.Build();

        var result = app.MapGrpcServicesWithReflection();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapGrpcServicesWithReflection_DevEnvironment_ReturnsSameApp()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Services.AddGrpcWithObservability(builder.Environment);

        var app    = builder.Build();
        var result = app.MapGrpcServicesWithReflection();

        result.Should().BeSameAs(app);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IHostEnvironment CreateEnv(string name)
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }
}
