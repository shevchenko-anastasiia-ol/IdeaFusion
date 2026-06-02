using ContentBLL.Services.Interfaces;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using FluentAssertions;
using GrpcClients.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using Moq;
using System.Net;
using Xunit;

namespace Content.Tests.Program;

// ── Custom IModelCustomizer: removes PostgreSQL-specific DEFAULT (now()) ──────
// Using ReplaceService avoids the DbContextOptions<T> constructor mismatch that
// arises when subclassing ContentDbContext with AddDbContext<TService, TImpl>.

file sealed class SqliteModelCustomizer : ModelCustomizer
{
    public SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies) { }

    public override void Customize(ModelBuilder modelBuilder, DbContext dbContext)
    {
        base.Customize(modelBuilder, dbContext);   // runs ContentDbContext.OnModelCreating
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entityType.GetProperties())
            if (property.GetDefaultValueSql() is "now()" or "false")
                property.SetDefaultValueSql(null);
    }
}

// ── Factory ───────────────────────────────────────────────────────────────────

public class ContentApiFactory : WebApplicationFactory<global::Program>
{
    // AddNpgSql evaluates the connection string eagerly (at startup) before
    // ConfigureAppConfiguration fires, so we must supply a non-null value via an
    // environment variable that WebApplication.CreateBuilder picks up immediately.
    private const string FakeConnStr = "Host=testhost;Port=5432;Database=test;Username=t;Password=t";

    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    public ContentApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__contentdb", FakeConnStr);
        _conn.Open();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _conn.Dispose();
            Environment.SetEnvironmentVariable("ConnectionStrings__contentdb", null);
        }
        base.Dispose(disposing);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Higher-priority source: overrides env-var at service-resolution time
                ["ConnectionStrings:contentdb"] = FakeConnStr,
                ["Minio:Endpoint"]              = "localhost:9000",
                ["Minio:AccessKey"]             = "minioadmin",
                ["Minio:SecretKey"]             = "minioadmin",
                ["Minio:BucketName"]            = "test",
            }));

        builder.ConfigureServices(services => SetupServices(services, _conn));
    }

    internal static void SetupServices(IServiceCollection services, SqliteConnection conn)
    {
        // Replace ContentDbContext with SQLite in-memory so ExecuteSqlRawAsync succeeds.
        // ReplaceService<IModelCustomizer> is used instead of a subclass to avoid the
        // DbContextOptions<T> generic mismatch that AddDbContext<TService,TImpl> creates.
        var remove = services
            .Where(d => d.ServiceType == typeof(ContentDbContext) ||
                        d.ServiceType == typeof(DbContextOptions<ContentDbContext>))
            .ToList();
        foreach (var d in remove) services.Remove(d);

        services.AddDbContext<ContentDbContext>(o =>
            o.UseSqlite(conn)
             .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());

        // Replace IUnitOfWork with a mock so its constructor never calls
        // ConnectionFactory.CreateConnection() (which tries to Open a real Postgres connection).
        // IConnectionFactory is intentionally NOT replaced so the Program.cs singleton lambda
        // remains as the last registration — needed for the IConnectionFactory throw-branch test.
        ReplaceWith<IUnitOfWork>(services, Mock.Of<IUnitOfWork>());

        // Replace IMinioClient: BucketExistsAsync → true so EnsureBucketAsync is a no-op
        ReplaceWith<IMinioClient>(services, BuildMinioMock());

        // Replace gRPC clients that require live network connections
        ReplaceWith<IUserGrpcClient>(services, Mock.Of<IUserGrpcClient>());
        ReplaceWith<ICollaborationRequestGrpcClient>(services, Mock.Of<ICollaborationRequestGrpcClient>());
    }

    internal static void ReplaceWith<T>(IServiceCollection services, T instance) where T : class
    {
        var stale = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in stale) services.Remove(d);
        services.AddSingleton(instance);
    }

    private static IMinioClient BuildMinioMock()
    {
        var m = new Mock<IMinioClient>();
        m.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);
        return m.Object;
    }
}

// ── Factory: Production environment (covers !IsDevelopment branch) ────────────

public class ContentApiProductionFactory : ContentApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Production");
    }
}

// ── Tests: Development ────────────────────────────────────────────────────────

[Collection("ContentProgramTests")]
public class ProgramTests : IClassFixture<ContentApiFactory>
{
    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public ProgramTests(ContentApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── Startup ───────────────────────────────────────────────────────────────

    [Fact]
    public void App_StartsSuccessfully()
    {
        _client.Should().NotBeNull();
    }

    // ── DI registrations ──────────────────────────────────────────────────────

    [Fact]
    public void DI_IPostService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IPostService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ICommentService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ICommentService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ILikeService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ILikeService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ISavedPostService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ISavedPostService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ITagService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ITagService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IPostViewService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IPostViewService>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IUnitOfWork_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IUnitOfWork>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ContentDbContext_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ContentDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void DI_IConnectionFactory_IsResolvable_WithValidConnectionString()
    {
        // Covers the "return new ConnectionFactory(connStr)" success branch in Program.cs
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IConnectionFactory>().Should().NotBeNull();
    }

    [Fact]
    public void DI_HealthCheckService_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider
             .GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()
             .Should().NotBeNull();
    }

    [Fact]
    public void DI_IUserGrpcClient_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<IUserGrpcClient>().Should().NotBeNull();
    }

    [Fact]
    public void DI_ICollaborationRequestGrpcClient_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ICollaborationRequestGrpcClient>().Should().NotBeNull();
    }

    // ── HTTP routes ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Http_PostController_IsMapped_ReturnsNon404()
    {
        var response = await _client.GetAsync("/api/post");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_HealthCheck_IsMapped_ReturnsNon404()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Http_UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/nonexistent-route-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── IConnectionFactory throw branch ───────────────────────────────────────

    [Fact]
    public void IConnectionFactory_ThrowsWhenConnectionStringIsEmpty()
    {
        // The child factory's ConfigureAppConfiguration fires before AddNpgSql at startup
        // (DelegatedWebApplicationFactory applies it earlier than the base factory).
        // AddNpgSql rejects null AND empty strings (Guard.ThrowIfNull with throwOnEmptyString:true),
        // but accepts whitespace-only strings.  The IConnectionFactory singleton uses
        // string.IsNullOrWhiteSpace which returns true for whitespace, so it throws.
        using var child = _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:contentdb"] = "  ",  // whitespace: passes AddNpgSql, triggers IsNullOrWhiteSpace guard
                })));

        using var scope = child.Services.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        act.Should().Throw<Exception>().WithMessage("*Connection string*");
    }
}

// ── Tests: Production environment (covers !IsDevelopment UseHttpsRedirection) ─

[Collection("ContentProgramTests")]
public class ProgramProductionTests : IClassFixture<ContentApiProductionFactory>
{
    private readonly ContentApiProductionFactory _factory;
    private readonly HttpClient _client;

    public ProgramProductionTests(ContentApiProductionFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public void App_StartsSuccessfully_InProductionEnvironment()
    {
        _client.Should().NotBeNull();
    }

    [Fact]
    public async Task Http_HealthCheck_IsMapped_InProduction_ReturnsNon404()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
