using System.Data;
using ContentDAL;
using ContentBLL;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using GrpcClients.Interfaces;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.Grpc.Users;
using IdeaFusion.GrpcClients.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
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
        var address = builder.Configuration["Grpc__IdentityService"] ?? "https://identityservice";
        o.Address = new Uri(address);
    })
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient>(o =>
    {
        var address = builder.Configuration["Grpc__CollaborationService"] ?? "https://collaborationservice";
        o.Address = new Uri(address);
    })
    .AddServiceDiscovery();

builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();
builder.Services.AddScoped<ICollaborationRequestGrpcClient, CollaborationRequestGrpcClient>();

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

builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddPermissionAuthorization();

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

app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();