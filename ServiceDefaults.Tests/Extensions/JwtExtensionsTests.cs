using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceDefaults.Extensions;
using System.Security.Claims;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class JwtExtensionsTests
{
    [Fact]
    public void AddKeycloakJwtAuthentication_WithDefaultConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_WithExplicitKeycloakUrl_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig("http://keycloak:8080", "myrealm");

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_WithEmptyUrl_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig(url: "", realm: "myrealm");

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Keycloak*");
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_WithWhitespaceRealm_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config   = BuildConfig(url: "http://keycloak:8080", realm: "  ");

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var result = services.AddKeycloakJwtAuthentication(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_DefaultConfig_UsesLocalhostFallback()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder().Build();

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().NotThrow("default fallback http://localhost:8080 should be used");
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_WithServicesKeycloakHttpConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services:keycloak:http:0"] = "http://keycloak:8080"
            })
            .Build();

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_UsesIdeaFusionRealmAndAudienceByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);

        options.Authority.Should().Be("http://localhost:8080/realms/IdeaFusion");
        options.Audience.Should().Be("ideafusion_api");
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_ReadsRolesFromFlatRoleClaim()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);

        // The realm's ideafusion-roles scope emits "role", not "realm_access.roles".
        options.TokenValidationParameters.RoleClaimType.Should().Be("role");
        options.MapInboundClaims.Should().BeFalse();
        options.TokenValidationParameters.ValidAudiences.Should().Equal("ideafusion_api");
    }

    // ── event callbacks ───────────────────────────────────────────────────────

    [Fact]
    public async Task OnAuthenticationFailed_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var ctx = new AuthenticationFailedContext(httpContext, scheme, options)
        {
            Exception = new Exception("auth failure")
        };

        var act = () => options.Events.OnAuthenticationFailed(ctx);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnTokenValidated_WithoutAuthorizationClaim_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var ctx = new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"))
        };

        var act = () => options.Events.OnTokenValidated(ctx);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnTokenValidated_WithRoleClaim_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var identity    = new ClaimsIdentity(new[] { new Claim("role", "moderator") }, "Bearer");
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var ctx = new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = new ClaimsPrincipal(identity)
        };

        var act = () => options.Events.OnTokenValidated(ctx);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnMessageReceived_HubsPathWithAccessToken_SetsToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.QueryString = new QueryString("?access_token=mytoken");
        httpContext.Request.Path        = "/hubs/chat";

        var ctx = new MessageReceivedContext(httpContext, scheme, options);
        await options.Events.OnMessageReceived(ctx);

        ctx.Token.Should().Be("mytoken");
    }

    [Fact]
    public async Task OnMessageReceived_NonHubsPath_DoesNotSetToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.QueryString = new QueryString("?access_token=mytoken");
        httpContext.Request.Path        = "/api/data";

        var ctx = new MessageReceivedContext(httpContext, scheme, options);
        await options.Events.OnMessageReceived(ctx);

        ctx.Token.Should().BeNull();
    }

    [Fact]
    public async Task OnMessageReceived_NoAccessToken_DoesNotSetToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.Path = "/hubs/chat";

        var ctx = new MessageReceivedContext(httpContext, scheme, options);
        await options.Events.OnMessageReceived(ctx);

        ctx.Token.Should().BeNull();
    }

    [Fact]
    public void AddKeycloakJwtAuthentication_WithExplicitAudience_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Url"]      = "http://keycloak:8080",
                ["Keycloak:Realm"]    = "myrealm",
                ["Keycloak:Audience"] = "myapi"
            })
            .Build();

        var act = () => services.AddKeycloakJwtAuthentication(config);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task OnTokenValidated_NullPrincipal_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var ctx = new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = null
        };

        var act = () => options.Events.OnTokenValidated(ctx);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnTokenValidated_WithNameClaim_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakJwtAuthentication(new ConfigurationBuilder().Build());
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                        .Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme  = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("preferred_username", "testuser"),
                new Claim("role", "author")
            }, "Bearer");
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var ctx = new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = new ClaimsPrincipal(identity)
        };

        var act = () => options.Events.OnTokenValidated(ctx);

        await act.Should().NotThrowAsync();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(string? url, string? realm)
    {
        var pairs = new Dictionary<string, string?>();
        if (url    is not null) pairs["Keycloak:Url"]   = url;
        if (realm  is not null) pairs["Keycloak:Realm"] = realm;
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
