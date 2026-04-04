using System.Data;
using ContentDAL;
using ContentBLL;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

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

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connStr = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connStr))
        throw new Exception("Connection string 'DefaultConnection' is missing!");

    return new ConnectionFactory(connStr);
});

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ContentDAL.Data.ContentDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database schema created/verified.");

        var seeder = new ContentDAL.Data.ContentDbSeed(dbContext,
            services.GetRequiredService<ILogger<ContentDAL.Data.ContentDbSeed>>());
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        int statusCode = exception switch
        {
            ContentDomain.Exception.NotFoundException => (int)HttpStatusCode.NotFound,
            ContentDomain.Exception.ValidationException => (int)HttpStatusCode.BadRequest,
            ContentDomain.Exception.BusinessConflictException => 409,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception?.Message,
            Detail = exception?.StackTrace
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Content API V1");
        c.RoutePrefix = string.Empty;
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();