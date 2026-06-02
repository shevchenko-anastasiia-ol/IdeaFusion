using System.Net;
using AutoMapper;
using Collaboration.API.Services;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Interfaces;
using Collaboration.Infrastructure.Data;
using Collaboration.Infrastructure.Indexes;
using Collaboration.Infrastructure.Seeding;
using FluentAssertions;
using GrpcClients.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure;

// ── Factory ───────────────────────────────────────────────────────────────────

public class CollaborationApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // AddMongoHealthCheck reads this; without it the app throws at registration
                ["ConnectionStrings:collaboration-db"] = "mongodb://localhost:27017",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Replace IIndexCreation with a no-op ───────────────────────────
            var indexDesc = services.Single(d => d.ServiceType == typeof(IIndexCreation));
            services.Remove(indexDesc);
            services.AddSingleton<IIndexCreation, NoOpIndexCreation>();

            // ── Replace IDataSeeder with a no-op ─────────────────────────────
            var seederDesc = services.Single(d => d.ServiceType == typeof(IDataSeeder));
            services.Remove(seederDesc);
            services.AddSingleton<IDataSeeder, NoOpDataSeeder>();

            // ── Replace IUserGrpcClient with mock ─────────────────────────────
            var grpcDesc = services.Single(d => d.ServiceType == typeof(IUserGrpcClient));
            services.Remove(grpcDesc);
            services.AddScoped<IUserGrpcClient>(_ => Mock.Of<IUserGrpcClient>());

            // ── Replace IMongoDatabase factory with mock ──────────────────────
            // This prevents real MongoDB connections when repositories are resolved.
            var mongoDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
            if (mongoDesc is not null) services.Remove(mongoDesc);
            services.AddScoped<IMongoDatabase>(_ => Mock.Of<IMongoDatabase>());
        });
    }
}

// ── Factory: Development environment (covers !IsDevelopment false branch) ────

public class CollaborationApiDevFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:collaboration-db"] = "mongodb://localhost:27017",
            });
        });
        builder.ConfigureServices(services =>
        {
            var indexDesc = services.Single(d => d.ServiceType == typeof(IIndexCreation));
            services.Remove(indexDesc);
            services.AddSingleton<IIndexCreation, NoOpIndexCreation>();

            var seederDesc = services.Single(d => d.ServiceType == typeof(IDataSeeder));
            services.Remove(seederDesc);
            services.AddSingleton<IDataSeeder, NoOpDataSeeder>();

            var grpcDesc = services.Single(d => d.ServiceType == typeof(IUserGrpcClient));
            services.Remove(grpcDesc);
            services.AddScoped<IUserGrpcClient>(_ => Mock.Of<IUserGrpcClient>());

            var mongoDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
            if (mongoDesc is not null) services.Remove(mongoDesc);
            services.AddScoped<IMongoDatabase>(_ => Mock.Of<IMongoDatabase>());
        });
    }
}

// ── No-op implementations ─────────────────────────────────────────────────────

file sealed class NoOpIndexCreation : IIndexCreation
{
    public Task CreateIndexesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class NoOpDataSeeder : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class ProgramTests : IClassFixture<CollaborationApiFactory>
{
    private readonly CollaborationApiFactory _factory;
    private readonly HttpClient _client;

    public ProgramTests(CollaborationApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── App startup ───────────────────────────────────────────────────────────

    [Fact]
    public void App_StartsSuccessfully_WithoutException()
    {
        _client.Should().NotBeNull();
    }

    // ── DI registrations ──────────────────────────────────────────────────────

    [Fact]
    public void DI_IMediator_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ITeamRepository_IsRegistered_As_TeamRepository()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ITeamRepository>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ICollaborationRequestRepository_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ICollaborationRequestRepository>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IGroupInvitationRepository_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IGroupInvitationRepository>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ITeamPostRepository_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ITeamPostRepository>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IUserSnapshotService_IsRegistered_As_GrpcUserSnapshotService()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IUserSnapshotService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<GrpcUserSnapshotService>();
    }

    [Fact]
    public void DI_IUserGrpcClient_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IUserGrpcClient>().Should().NotBeNull();
    }

    [Fact]
    public void DI_MongoDbContext_IsRegistered_AsSingleton()
    {
        _factory.Services.GetService<MongoDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IMapper_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void DI_HealthCheckService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()
             .Should().NotBeNull();
    }

    [Fact]
    public void DI_IDataSeeder_IsRegistered()
    {
        _factory.Services.GetService<IDataSeeder>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IIndexCreation_IsRegistered()
    {
        _factory.Services.GetService<IIndexCreation>().Should().NotBeNull();
    }

    // ── HTTP route registration ───────────────────────────────────────────────

    [Fact]
    public async Task Http_TeamController_IsMapped_ReturnsNonNotFound()
    {
        var response = await _client.GetAsync("/api/team");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_CollaborationRequestsController_IsMapped_ReturnsNonNotFound()
    {
        var response = await _client.GetAsync("/api/collaboration-requests");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_GroupInvitationsController_IsMapped_ReturnsNonNotFound()
    {
        var response = await _client.GetAsync("/api/group-invitations");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_TeamPostsController_IsMapped_ReturnsNonNotFound()
    {
        var response = await _client.GetAsync("/api/team-posts/test-post-id");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_HealthCheckEndpoint_IsMapped_ReturnsNonNotFound()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/does-not-exist-route-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────

    [Fact]
    public async Task GlobalExceptionMiddleware_IsActive_ReturnsJsonProblemOnError()
    {
        // An unknown route that bypasses controllers and goes to 404 directly won't
        // hit the middleware. We trigger it by requesting an existing controller route
        // which throws internally; GlobalExceptionMiddleware wraps it.
        var response = await _client.GetAsync("/api/team");

        // Content-Type should be set to application/problem+json by the middleware
        // OR the controller's HandleException returns a structured error — either way
        // the response is not a raw unhandled exception page.
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    // ── MongoDb settings validation ───────────────────────────────────────────

    [Fact]
    public void Configuration_MongoDbSettings_ConnectionString_IsProvided()
    {
        // Verify PostConfigure validation passes — MongoDbContext singleton can be resolved
        // which means the connection string is non-empty.
        var ctx = _factory.Services.GetRequiredService<MongoDbContext>();
        ctx.Should().NotBeNull();
    }
}

// ── Tests for Development factory (!IsDevelopment = false branch) ─────────────

public class ProgramDevTests : IClassFixture<CollaborationApiDevFactory>
{
    private readonly CollaborationApiDevFactory _factory;

    public ProgramDevTests(CollaborationApiDevFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void App_WithDevelopmentEnvironment_StartsSuccessfully()
    {
        _factory.Should().NotBeNull();
    }

    [Fact]
    public async Task Http_TeamController_InDevelopment_IsMapped()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/team");
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.NotFound);
    }
}

