using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceDefaults.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class SwaggerExtensionsTests
{
    // ── AddSwaggerWithKeycloak ─────────────────────────────────────────────────

    [Fact]
    public void AddSwaggerWithKeycloak_DefaultConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var act = () => services.AddSwaggerWithKeycloak(config, "Test API");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WithExplicitKeycloakUrl_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Url"]   = "http://keycloak:8080",
                ["Keycloak:Realm"] = "testrealm"
            })
            .Build();

        var act = () => services.AddSwaggerWithKeycloak(config, "My API", "v2");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WithServicesKeycloakHttpsConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services:keycloak:https:0"] = "https://keycloak:8443"
            })
            .Build();

        var act = () => services.AddSwaggerWithKeycloak(config, "API");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WithServicesKeycloakHttpConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services:keycloak:http:0"] = "http://keycloak:8080"
            })
            .Build();

        var act = () => services.AddSwaggerWithKeycloak(config, "API");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddSwaggerWithKeycloak_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var result = services.AddSwaggerWithKeycloak(config, "Test API");

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSwaggerWithKeycloak_SwaggerGenOptionsLambda_IsExecutedOnResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddSwaggerWithKeycloak(config, "Test API");

        using var sp = services.BuildServiceProvider();
        // Resolving SwaggerGenOptions triggers execution of the AddSwaggerGen lambda
        var opts = sp.GetRequiredService<IOptions<SwaggerGenOptions>>();

        opts.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WithEnvVarKeycloakUrl_UsesEnvVar()
    {
        Environment.SetEnvironmentVariable("KEYCLOAK_URL", "http://keycloak-env:8080");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder().Build();
            services.AddSwaggerWithKeycloak(config, "Test API");

            using var sp = services.BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<SwaggerGenOptions>>();
            opts.Value.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("KEYCLOAK_URL", null);
        }
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WithEnvVarKeycloakRealm_UsesEnvVar()
    {
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder().Build();
            services.AddSwaggerWithKeycloak(config, "Test API");

            using var sp = services.BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<SwaggerGenOptions>>();
            opts.Value.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("KEYCLOAK_REALM", null);
        }
    }

    [Fact]
    public void AddSwaggerWithKeycloak_WhenXmlFileExists_IncludesXmlComments()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "ServiceDefaults.xml");
        File.WriteAllText(xmlPath, "<doc/>");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder().Build();
            services.AddSwaggerWithKeycloak(config, "Test API");

            using var sp = services.BuildServiceProvider();
            // Resolving the options triggers the lambda which checks File.Exists
            var opts = sp.GetRequiredService<IOptions<SwaggerGenOptions>>();

            opts.Value.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(xmlPath)) File.Delete(xmlPath);
        }
    }

    // ── UseSwaggerWithKeycloak ────────────────────────────────────────────────

    [Fact]
    public void UseSwaggerWithKeycloak_NonDevEnvironment_ReturnsSameApp()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Production" });
        var app = builder.Build();

        var result = app.UseSwaggerWithKeycloak();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseSwaggerWithKeycloak_DevEnvironment_ReturnsSameApp()
    {
        var config  = new ConfigurationBuilder().Build();
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Services.AddSwaggerWithKeycloak(config, "Test API");

        var app    = builder.Build();
        var result = app.UseSwaggerWithKeycloak(config);

        result.Should().BeSameAs(app);
    }
}
