using IdentityAPI.GrpcServices;
using IdentityAPI.Keycloak;
using IdentityBLL.Interfaces;
using IdentityBLL.MappingProfiles;
using IdentityBLL.Services;
using IdentityDAL.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using ServiceDefaults.Extensions;
using ServiceDefaults.Health;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();

var connectionString = builder.Configuration.GetConnectionString("identityservicedb")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString!);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Identity here means "the ASP.NET Identity user store", not authentication.
// Keycloak owns credentials, roles and sessions; this registration exists only so
// UserManager/RoleManager can read and write profile rows. AddIdentityCore is used
// instead of AddIdentity because it does not install cookie schemes, which would
// otherwise fight the Keycloak bearer scheme for the default.
// Keycloak is the source of truth for account uniqueness, and the same email can
// legitimately appear on more than one row during migration.
builder.Services.Configure<IdentityOptions>(options => options.User.RequireUniqueEmail = false);

builder.Services
    .AddIdentity<IdentityServiceDomain.Entities.ApplicationUser, IdentityServiceDomain.Entities.ApplicationRole>()
    .AddEntityFrameworkStores<IdentityDbContext>();

// Every service validates the same Keycloak access token: authority-based, no local
// signing key, roles from the flat "role" claim.
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// AddIdentity installs cookie schemes and points the defaults at them. Keycloak has
// to win, and PostConfigure runs after every Configure callback, so this holds
// regardless of registration order.
builder.Services.PostConfigure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
});

// Fail-closed fallback plus the shared RequireAuthor/Moderator/Administrator policies.
builder.Services.AddIdeaFusionAuthorization();

builder.Services.Configure<KeycloakAdminOptions>(builder.Configuration.GetSection(KeycloakAdminOptions.SectionName));
// KeycloakAdminClient sends paths relative to the realm's admin root (users/{id}, roles/{name});
// without a base address every admin call fails with "invalid request URI".
builder.Services.AddHttpClient(KeycloakAdminClient.HttpClientName, client =>
{
    var keycloakUrl = builder.Configuration["Keycloak:Url"]
                      ?? Environment.GetEnvironmentVariable("KEYCLOAK_URL")
                      ?? "http://localhost:8080";
    var realm = builder.Configuration["Keycloak:Realm"]
                ?? Environment.GetEnvironmentVariable("KEYCLOAK_REALM")
                ?? "IdeaFusion";
    client.BaseAddress = new Uri($"{keycloakUrl.TrimEnd('/')}/admin/realms/{realm}/");
});
builder.Services.AddScoped<IKeycloakAdminClient, KeycloakAdminClient>();

builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
        builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
    .WithSSL(false)
    .Build());

builder.Services.AddGrpcWithObservability(builder.Environment);

builder.Services.AddAutoMapperWithLogging(typeof(IdentityProfile).Assembly);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithKeycloak(builder.Configuration, "IdeaFusion Identity API");

builder.Services.AddHealthChecks()
    .AddPostgresHealthCheck(
        configuration: builder.Configuration,
        connectionName: "identityservicedb",
        serviceName: "identityservice",
        timeoutSeconds: 5);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    if (dbContext.Database.IsRelational())
        await dbContext.Database.MigrateAsync();
    else
        await dbContext.Database.EnsureCreatedAsync();
}

app.UseSwaggerWithKeycloak(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGrpcService<UserGrpcServiceImpl>();

app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Probes must stay reachable without a token, so they opt out of the fallback policy.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program { }
