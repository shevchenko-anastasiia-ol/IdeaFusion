using IdentityDAL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Moq;

namespace Identity.Tests.Integration;

public class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting injects individual values into IWebHostBuilder before
        // WebApplication.CreateBuilder processes configuration — visible during service registration.
        var settings = new Dictionary<string, string>
        {
            ["JwtSettings:Key"]                        = "super-secret-test-key-for-integration-tests-only-long-enough",
            ["JwtSettings:Issuer"]                     = "test-issuer",
            ["JwtSettings:Audience"]                   = "test-audience",
            ["JwtSettings:AccessTokenDurationMinutes"] = "15",
            ["JwtSettings:RefreshTokenDurationDays"]   = "7",
            ["Minio:Endpoint"]  = "localhost:9000",
            ["Minio:AccessKey"] = "minioadmin",
            ["Minio:SecretKey"] = "minioadmin",
            // Required by AddPostgresHealthCheck; the check itself is never called in tests
            ["ConnectionStrings:identityservicedb"] =
                "Host=localhost;Port=5432;Database=test_identity;Username=test;Password=test",
        };

        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(settings.ToDictionary(k => k.Key, v => (string?)v.Value)));

        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL options with InMemory.
            // We only swap out DbContextOptions<T> so the existing scoped IdentityDbContext
            // registration still works, avoiding the "two EF providers" conflict.
            var optDesc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (optDesc != null)
                services.Remove(optDesc);

            services.AddSingleton(
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseInMemoryDatabase("IdentityIntegrationTest-" + Guid.NewGuid())
                    .Options);

            // Replace MinIO with a no-op mock
            var minioDescriptors = services
                .Where(d => d.ServiceType == typeof(IMinioClient))
                .ToList();
            foreach (var d in minioDescriptors)
                services.Remove(d);

            services.AddSingleton<IMinioClient>(_ => new Mock<IMinioClient>().Object);
        });
    }
}
