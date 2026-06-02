using Collaboration.Domain.Interfaces;
using Collaboration.Infrastructure.Indexes;
using Collaboration.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Collaboration.Application.Tests.Integration;

/// <summary>
/// Spins up the Collaboration API in-process with all MongoDB infrastructure
/// replaced by Moq mocks, so no real MongoDB is required.
/// Exposed mocks allow individual tests to configure return values.
/// </summary>
public class CollaborationWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ITeamRepository>                  TeamRepoMock                  { get; } = new(MockBehavior.Loose);
    public Mock<ICollaborationRequestRepository>  CollabRequestRepoMock         { get; } = new(MockBehavior.Loose);
    public Mock<IGroupInvitationRepository>       GroupInvitationRepoMock       { get; } = new(MockBehavior.Loose);
    public Mock<ITeamPostRepository>              TeamPostRepoMock               { get; } = new(MockBehavior.Loose);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var settings = new Dictionary<string, string>
        {
            // MongoDB connection string — satisfies PostConfigure validation without connecting
            ["ConnectionStrings:collaboration-db"]    = "mongodb://localhost:27017",
            ["MongoDbSettings:ConnectionString"]      = "mongodb://localhost:27017",
            ["MongoDbSettings:DatabaseName"]          = "test-collab",
            ["MongoDbSettings:MaxConnectionPoolSize"] = "1",
            ["MongoDbSettings:MinConnectionPoolSize"] = "0",
            ["MongoDbSettings:ConnectTimeoutSeconds"] = "1",
            ["MongoDbSettings:SocketTimeoutSeconds"]  = "1",
            ["Keycloak:Url"]      = "http://localhost:8080",
            ["Keycloak:Realm"]    = "test",
            ["Keycloak:Audience"] = "test",
        };

        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(settings.ToDictionary(k => k.Key, v => (string?)v.Value)));

        builder.ConfigureServices(services =>
        {
            // ── Replace all MongoDB repositories with mocks ──────────────────
            ReplaceScoped<ITeamRepository>(services, TeamRepoMock.Object);
            ReplaceScoped<ICollaborationRequestRepository>(services, CollabRequestRepoMock.Object);
            ReplaceScoped<IGroupInvitationRepository>(services, GroupInvitationRepoMock.Object);
            ReplaceScoped<ITeamPostRepository>(services, TeamPostRepoMock.Object);

            // ── Replace startup singletons that try to reach MongoDB ──────────
            var indexTypes  = services.Where(d => d.ServiceType == typeof(IIndexCreation)).ToList();
            var seederTypes = services.Where(d => d.ServiceType == typeof(IDataSeeder)).ToList();
            foreach (var d in indexTypes)  services.Remove(d);
            foreach (var d in seederTypes) services.Remove(d);

            services.AddSingleton<IIndexCreation>(_ => new Mock<IIndexCreation>().Object);
            services.AddSingleton<IDataSeeder>(_ => new Mock<IDataSeeder>().Object);
        });
    }

    private static void ReplaceScoped<TService>(IServiceCollection services, TService implementation)
        where TService : class
    {
        var existing = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var d in existing)
            services.Remove(d);
        services.AddScoped<TService>(_ => implementation);
    }
}
