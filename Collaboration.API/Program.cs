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
using FluentValidation;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

// ВАЖЛИВО: має бути першим — до будь-якої ініціалізації MongoDB
MongoMappings.Register();

var builder = WebApplication.CreateBuilder(args);

var mongoConn = builder.Configuration.GetConnectionString("Collaboration")
                ?? builder.Configuration.GetSection("MongoDbSettings").GetValue<string>("ConnectionString");

builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoConn;
    options.DatabaseName = "collaboration-db";
    options.MaxConnectionPoolSize = 100;
    options.MinConnectionPoolSize = 5;
    options.ConnectTimeoutSeconds = 10;
    options.SocketTimeoutSeconds = 10;
});

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
builder.Services.AddScoped<IDataSeeder, DatabaseSeeder>();

// --- MediatR ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(BaseApiController).Assembly);
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
    cfg.RegisterServicesFromAssemblies(typeof(GetCollaborationRequestsByTeamQueryHandler).Assembly);
});

// --- Validators ---
builder.Services.AddValidatorsFromAssembly(typeof(BaseApiController).Assembly);

// --- Memory Cache (для CachingBehavior) ---
builder.Services.AddMemoryCache();

// --- Controllers ---
builder.Services.AddControllers();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Collaboration API",
        Version = "v1"
    });
});

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => new MongoClient(mongoConn),
        name: "collaboration-db",
        timeout: TimeSpan.FromSeconds(5)
    );

var app = builder.Build();

// --- MongoDB indexes ---
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Collaboration API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();