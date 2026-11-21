using System.Data;
using ContentDAL;
using ContentBLL;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using ContentAPI.Services;
using GrpcClients.Interfaces;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.Grpc.Users;
using IdeaFusion.GrpcClients.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using ContentAPI.CopyDetection;
using ContentAPI.Middleware;
using ServiceDefaults.Extensions;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.NpgSql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();
builder.Services.AddServiceDiscovery();

builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
    {
        // Aspire registers IdentityAPI's endpoints under the names "identity-http"/"identity-https",
        // so the plain "https://identityservice" never resolved (gRPC went to DNS and failed with
        // "No such host"). "_endpoint.service" picks the named endpoint. The env var Grpc__X is
        // read as the configuration key Grpc:X.
        var address = builder.Configuration["Grpc:IdentityService"] ?? "https://_identity-https.identityservice";
        o.Address = new Uri(address);
    })
    // IdentityAPI and Collaboration.API accept only authenticated calls, gRPC included, and these
    // clients sent no token: every call came back 401 (moderation audit entries were lost, among
    // others). The caller's own bearer token is forwarded, as in Collaboration.API.
    .AddCallCredentials((_, metadata, services) =>
    {
        var authorization = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            metadata.Add("Authorization", authorization);
        return Task.CompletedTask;
    })
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient>(o =>
    {
        var address = builder.Configuration["Grpc:CollaborationService"] ?? "https://_collaborations-https.collaborationservice";
        o.Address = new Uri(address);
    })
    // IdentityAPI and Collaboration.API accept only authenticated calls, gRPC included, and these
    // clients sent no token: every call came back 401 (moderation audit entries were lost, among
    // others). The caller's own bearer token is forwarded, as in Collaboration.API.
    .AddCallCredentials((_, metadata, services) =>
    {
        var authorization = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            metadata.Add("Authorization", authorization);
        return Task.CompletedTask;
    })
    .AddServiceDiscovery();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();
builder.Services.AddScoped<ICollaborationRequestGrpcClient, CollaborationRequestGrpcClient>();
builder.Services.AddScoped<IContentAuditWriter, ContentAuditWriter>();

// Moderation letters to authors (a post confirmed as a copy), sent off the request.
builder.Services.Configure<ContentAPI.Notifications.ContentEmailOptions>(
    builder.Configuration.GetSection(ContentAPI.Notifications.ContentEmailOptions.Section));
builder.Services.AddSingleton<ContentAPI.Notifications.ModerationMailQueue>();
builder.Services.AddScoped<ContentAPI.Notifications.IModerationMailer, ContentAPI.Notifications.ModerationMailer>();
builder.Services.AddHostedService<ContentAPI.Notifications.ModerationMailSender>();
// Comment letters: the recipient's "Коментарі" switch lives in CollaborationService.
builder.Services.AddHttpClient(ContentAPI.Notifications.CommentMailer.SettingsClient,
        c => c.BaseAddress = new Uri("https+http://collaborationservice"))
    .AddServiceDiscovery();
builder.Services.AddScoped<ContentAPI.Notifications.ICommentMailer, ContentAPI.Notifications.CommentMailer>();

// MinIO must be registered BEFORE AddDataAccess, because UnitOfWork depends on IMinioClient
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
        builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
    .WithSSL(false)
    .Build());

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusinessLayer(builder.Configuration);
// Image copy detection (ml/service): decorates IPostService, so it goes after AddBusinessLayer.
builder.Services.AddCopyDetection(builder.Configuration);

builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddIdeaFusionAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithKeycloak(builder.Configuration, "Content API");

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("contentdb")));

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connStr = configuration.GetConnectionString("contentdb");

    if (string.IsNullOrWhiteSpace(connStr))
        throw new Exception("Connection string 'DefaultConnection' is missing!");

    return new ConnectionFactory(connStr);
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("contentdb")!,
        name: "postgresql",
        tags: ["db", "ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ContentDAL.Data.ContentDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database schema created/verified.");

        // Apply pending schema changes that EnsureCreated doesn't handle
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE collaborationsnapshots ADD COLUMN IF NOT EXISTS externalid character varying(100);");

        // PostAuthors.externalid ties a content author to a Keycloak subject. The
        // unique index is filtered so the many demo rows can keep a null value.
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE postauthors ADD COLUMN IF NOT EXISTS externalid character varying(100);");
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS ix_postauthors_externalid ON postauthors (externalid) WHERE externalid IS NOT NULL;");
        logger.LogInformation("Schema patches applied.");

        var seeder = new ContentDAL.Data.ContentDbSeed(dbContext,
            services.GetRequiredService<ILogger<ContentDAL.Data.ContentDbSeed>>());
        await seeder.SeedAsync();

        var realSeeder = new ContentDAL.Data.RealDataSeeder(
            dbContext,
            services.GetRequiredService<IMinioClient>(),
            services.GetRequiredService<ILogger<ContentDAL.Data.RealDataSeeder>>());
        await realSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.UseSwaggerWithKeycloak();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Registered before routing so ownership and not-found failures from any endpoint
// come back as a status code instead of a 500.
app.UseGlobalExceptionHandling();

app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program { }