using System.Text.Json;
using IdentityAPI.Keycloak;
using IdentityDAL.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;

namespace Identity.Tests.Integration;

public class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>The Keycloak admin client is replaced so no test ever calls the real realm.</summary>
    public Mock<IKeycloakAdminClient> KeycloakAdminMock { get; } = new();

    /// <summary>Realm roles the mocked admin client reports after a role change.</summary>
    public List<string> KeycloakRoles { get; } = new() { "author" };

    public IdentityWebApplicationFactory() => SetUpDefaults();

    /// <summary>
    /// (Re)installs the mocked realm's default behaviour. Called from the
    /// constructor and again by <see cref="ResetAsync"/>: a test that makes the
    /// mock throw or return a different value would otherwise keep that setup for
    /// the rest of the class, because Moq's last-registered setup wins and the
    /// mock lives as long as the shared factory.
    /// </summary>
    private void SetUpDefaults()
    {
        KeycloakAdminMock.Setup(c => c.AccountConsoleUrl)
            .Returns("https://keycloak.test/realms/IdeaFusion/account/");

        KeycloakAdminMock.Setup(c => c.IsOtpConfiguredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        KeycloakAdminMock.Setup(c => c.GetUserRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => KeycloakRoles.ToList());

        KeycloakAdminMock.Setup(c => c.SearchUsersAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? search, int first, int max, CancellationToken _) =>
            {
                IEnumerable<KeycloakUserSummary> all =
                [
                    new KeycloakUserSummary
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Username = "AnnoGray",
                        Email = "anna@ideafusion.com",
                        EmailVerified = true,
                        Enabled = true,
                        RealmRoles = ["author"],
                        TwoFactorConfigured = false
                    },
                    new KeycloakUserSummary
                    {
                        Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        Username = "admin",
                        Email = "admin@admin.com",
                        EmailVerified = true,
                        Enabled = true,
                        RealmRoles = ["administrator", "author"],
                        TwoFactorConfigured = true
                    }
                ];

                if (!string.IsNullOrWhiteSpace(search))
                    all = all.Where(u =>
                        (u.Username?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

                return all.Skip(first).Take(max).ToList();
            });

        KeycloakAdminMock.Setup(c => c.AddRolesAsync(
                It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IEnumerable<string> roles, CancellationToken __) =>
            {
                foreach (var r in roles) if (!KeycloakRoles.Contains(r)) KeycloakRoles.Add(r);
                return KeycloakRoles.ToList();
            });

        KeycloakAdminMock.Setup(c => c.RemoveRolesAsync(
                It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IEnumerable<string> roles, CancellationToken __) =>
            {
                foreach (var r in roles) KeycloakRoles.Remove(r);
                return KeycloakRoles.ToList();
            });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting injects individual values into IWebHostBuilder before
        // WebApplication.CreateBuilder processes configuration — visible during service registration.
        var settings = new Dictionary<string, string>
        {
            ["Minio:Endpoint"]  = "localhost:9000",
            ["Minio:AccessKey"] = "minioadmin",
            ["Minio:SecretKey"] = "minioadmin",
            ["Keycloak:Url"]      = "https://keycloak.test",
            ["Keycloak:Realm"]    = "IdeaFusion",
            ["Keycloak:Audience"] = "ideafusion_api",
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
            // Replace Keycloak JWT validation with a header-driven principal.
            // PostConfigure runs after AddIdentity's cookie defaults and after
            // AddKeycloakAuthentication, so the test scheme always wins.
            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
                o.DefaultScheme = TestAuthHandler.Scheme;
            });

            // Replace the Keycloak admin client: no test may reach a real realm.
            services.RemoveAll<IKeycloakAdminClient>();
            services.AddSingleton(KeycloakAdminMock.Object);

            // Replace PostgreSQL options with InMemory.
            // We only swap out DbContextOptions<T> so the existing scoped IdentityDbContext
            // registration still works, avoiding the "two EF providers" conflict.
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.AddSingleton(
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseInMemoryDatabase("IdentityIntegrationTest-" + Guid.NewGuid())
                    .Options);

            // Replace MinIO with a no-op mock
            services.RemoveAll<IMinioClient>();
            services.AddSingleton<IMinioClient>(_ => new Mock<IMinioClient>().Object);
        });
    }

    /// <summary>
    /// Returns the shared factory to a known state.
    /// <para>
    /// A <see cref="WebApplicationFactory{TEntryPoint}"/> is built once per test
    /// class, so the InMemory store and the mocked realm roles are shared by every
    /// test in it. Without this reset a test that asserts "exactly one audit row"
    /// would see whatever its siblings left behind, and the run would depend on
    /// xUnit's collection order.
    /// </para>
    /// </summary>
    public async Task ResetAsync()
    {
        SetUpDefaults();

        KeycloakRoles.Clear();
        KeycloakRoles.Add("author");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        db.AuditLogEntries.RemoveRange(db.AuditLogEntries);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// ASP.NET Core writes camelCase; the DTOs use PascalCase. Tests deserialize the
/// same way a strongly typed client would, so case matching has to be on or every
/// property silently binds to its default value.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), Options)
           ?? throw new InvalidOperationException($"Could not read a {typeof(T).Name} from the response.");
}
