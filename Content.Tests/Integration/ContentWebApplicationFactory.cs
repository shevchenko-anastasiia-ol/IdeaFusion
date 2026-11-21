using ContentBLL.Services.Interfaces;
using ContentDAL.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Moq;

namespace Content.Tests.Integration;

/// <summary>
/// Spins up ContentAPI in-process with InMemory EF, mocked MinIO,
/// and exposed BLL-service mocks so each test can configure expectations.
/// </summary>
public class ContentWebApplicationFactory : WebApplicationFactory<global::Program>
{
    public Mock<IPostService>      PostServiceMock      { get; } = new(MockBehavior.Loose);
    public Mock<ICommentService>   CommentServiceMock   { get; } = new(MockBehavior.Loose);
    public Mock<ILikeService>      LikeServiceMock      { get; } = new(MockBehavior.Loose);
    public Mock<ITagService>       TagServiceMock       { get; } = new(MockBehavior.Loose);
    public Mock<ISavedPostService> SavedPostServiceMock { get; } = new(MockBehavior.Loose);
    public Mock<IPostViewService>  PostViewServiceMock  { get; } = new(MockBehavior.Loose);
    public Mock<IPostAuthorResolver> PostAuthorResolverMock { get; } = new(MockBehavior.Loose);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings:contentdb"] =
                "Host=localhost;Port=5432;Database=test_content;Username=test;Password=test",
            ["Minio:Endpoint"]    = "localhost:9000",
            ["Minio:AccessKey"]   = "minioadmin",
            ["Minio:SecretKey"]   = "minioadmin",
            ["Minio:BucketName"]  = "content",
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
            // ── Test auth ──────────────────────────────────────────────────────
            // PostConfigure runs after every Configure callback, including the
            // app's own AddAuthentication(...), so the test scheme reliably wins.
            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });

            // ── InMemory EF ──────────────────────────────────────────────────
            // ContentAPI registers ContentDbContext twice (AddDataAccess + Program.cs directly),
            // both with Npgsql. Calling AddDbContext a third time with InMemory causes the
            // "two providers" error. Fix: swap only DbContextOptions<T> and force a dedicated
            // internal EF service provider so Npgsql and InMemory stay isolated.
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ContentDbContext>))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddSingleton(
                new DbContextOptionsBuilder<ContentDbContext>()
                    .UseInMemoryDatabase("ContentIntegrationTest-" + Guid.NewGuid())
                    .UseInternalServiceProvider(inMemoryProvider)
                    .Options);

            // ── Mock MinIO ────────────────────────────────────────────────────
            var minioDescriptors = services
                .Where(d => d.ServiceType == typeof(IMinioClient))
                .ToList();
            foreach (var d in minioDescriptors) services.Remove(d);

            services.AddSingleton<IMinioClient>(_ => new Mock<IMinioClient>().Object);

            // ── Mock all BLL services ─────────────────────────────────────────
            services.AddScoped<IPostService>(_      => PostServiceMock.Object);
            services.AddScoped<ICommentService>(_   => CommentServiceMock.Object);
            services.AddScoped<ILikeService>(_      => LikeServiceMock.Object);
            services.AddScoped<ITagService>(_       => TagServiceMock.Object);
            services.AddScoped<ISavedPostService>(_ => SavedPostServiceMock.Object);
            services.AddScoped<IPostViewService>(_  => PostViewServiceMock.Object);
            // The caller is resolved from the token, so endpoints that answer
            // "which author am I" need this mocked rather than a service method.
            services.AddScoped<IPostAuthorResolver>(_ => PostAuthorResolverMock.Object);
        });
    }
}
