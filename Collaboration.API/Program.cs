using Collaboration.API;
using Collaboration.API.Controllers;
using Collaboration.API.Middleware;
using Collaboration.Application.Behaviors;
using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Domain.Interfaces;
using Collaboration.Infrastructure.Data;
using Collaboration.Infrastructure.Indexes;
using Collaboration.Infrastructure.Mapping;
using Collaboration.Infrastructure.Repositories;
using Collaboration.Infrastructure.Seeding;
using CollaborationGrpcService.MappingProfiles;
using CollaborationGrpcService.Services;
using FluentValidation;
using GrpcClients.Interfaces;
using IdeaFusion.Grpc.Teams;
using IdeaFusion.Grpc.Users;
using IdeaFusion.GrpcClients.Clients;
using ServiceDefaults.Extensions;
using ServiceDefaults.Health;
using MongoDB.Driver;

// ВАЖЛИВО: має бути першим — до будь-якої ініціалізації MongoDB
MongoMappings.Register();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();
builder.Services.AddGrpcWithObservability(builder.Environment);
builder.Services.AddServiceDiscovery();

// після builder.Services.AddServiceDiscovery();

builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
    {
        o.Address = new Uri("https+http://identityservice");
    })
    .AddServiceDiscovery();

builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();

// --- MongoDB ---
var aspireConn = builder.Configuration.GetConnectionString("collaboration-db")
                ?? builder.Configuration.GetConnectionString("mongodb");

if (!string.IsNullOrEmpty(aspireConn))
{
    builder.Services.Configure<MongoDbSettings>(options =>
    {
        options.ConnectionString = aspireConn;
        options.DatabaseName = "collaboration-db";
        options.MaxConnectionPoolSize = 100;
        options.MinConnectionPoolSize = 5;
        options.ConnectTimeoutSeconds = 10;
        options.SocketTimeoutSeconds = 10;
    });
}
else
{
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection("MongoDbSettings"));
}

var mongoConnString = !string.IsNullOrEmpty(aspireConn)
    ? aspireConn
    : builder.Configuration.GetSection("MongoDbSettings").GetValue<string>("ConnectionString");

builder.Services.AddMongoDbTelemetry(mongoConnString!);

builder.Services.PostConfigure<MongoDbSettings>(options =>
{
    if (string.IsNullOrEmpty(options.ConnectionString))
        throw new InvalidOperationException("MongoDB ConnectionString is required");
    if (string.IsNullOrEmpty(options.DatabaseName))
        throw new InvalidOperationException("MongoDB DatabaseName is required");
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped(sp =>
{
    var context = sp.GetRequiredService<MongoDbContext>();
    return context.Database;
});

// --- Repositories ---
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ICollaborationRequestRepository, CollaborationRequestRepository>();
builder.Services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>();
builder.Services.AddScoped<ITeamPostRepository, TeamPostRepository>();

// --- Infrastructure ---
builder.Services.AddSingleton<IIndexCreation, MongoIndexCreation>();
builder.Services.AddSingleton<IDataSeeder, DatabaseSeeder>();

// --- Auth ---
builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddPermissionAuthorization();

// --- AutoMapper ---
builder.Services.AddAutoMapperWithLogging(
    typeof(CollaborationGrpcProfile).Assembly
);

// --- MediatR ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(BaseApiController).Assembly);
    cfg.RegisterServicesFromAssemblies(typeof(GetCollaborationRequestsByTeamQueryHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
});

// --- Validators ---
builder.Services.AddValidatorsFromAssembly(typeof(BaseApiController).Assembly);

// --- Memory Cache (для CachingBehavior) ---
builder.Services.AddMemoryCache();

// --- Controllers ---
builder.Services.AddControllers();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithKeycloak(builder.Configuration, "Collaboration API");

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddMongoHealthCheck(
        builder.Configuration,
        connectionName: "collaboration-db",
        serviceName: "collaborationservice",
        databaseName: "collaboration-db",
        timeoutSeconds: 5);

var app = builder.Build();

// --- MongoDB indexes + seeding ---
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Creating MongoDB indexes...");
        var indexService = scope.ServiceProvider.GetRequiredService<IIndexCreation>();
        await indexService.CreateIndexesAsync();
        logger.LogInformation("MongoDB indexes created successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating MongoDB indexes. Continuing anyway...");
    }

    try
    {
        logger.LogInformation("Starting database seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database seeding. Application will continue, but database may be empty.");
    }
}

// --- Middleware ---
app.UseSwaggerWithKeycloak();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.MapGrpcService<CollaborationRequestGrpcServiceImpl>();
app.MapGrpcServicesWithReflection();

await app.RunAsync();