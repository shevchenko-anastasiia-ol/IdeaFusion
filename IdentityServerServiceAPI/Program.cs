using IdeaFusion.IdentityService.Api;
using IdentityServerServiceAPI.Data;
using IdentityServerService.Entities;
using ServiceDefaults.Extensions;
using IdentityServerService.Entities;
using IdentityServerServiceAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();

var connectionString = builder.Configuration.GetConnectionString("ideafusion-identityservice-db")
                      ?? builder.Configuration.GetConnectionString("DefaultConnection");

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    options.IssuerUri = "https://localhost:7052";
    options.EmitStaticAudienceClaim = true;
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddAspNetIdentity<ApplicationUser>()
    .AddConfigurationStore(opt =>
    {
        opt.ConfigureDbContext = db =>
            db.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(migrationsAssembly));
    })
    .AddOperationalStore(opt =>
    {
        opt.ConfigureDbContext = db =>
            db.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(migrationsAssembly));
        opt.EnableTokenCleanup = true;
        opt.TokenCleanupInterval = 3600;
    })
    .AddDeveloperSigningCredential(true);

builder.Services.AddRazorPages();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IdeaFusion IdentityService",
        Version = "v1",
        Description = "Authorization server for IdeaFusion platform"
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "OAuth2 Authorization Code flow via IdentityServer",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://localhost:7052/connect/authorize"),
                TokenUrl = new Uri("https://localhost:7052/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
                    { "email", "Email information" },
                    { "ideafusion_api", "Access IdeaFusion API" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid", "profile", "email", "ideafusion_api" }
        }
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IdeaFusion IdentityService v1");

        options.OAuthClientId("swagger");
        options.OAuthAppName("IdeaFusion IdentityService Swagger UI");
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile", "email", "ideafusion_api");
        options.OAuthScopeSeparator(" ");
        options.EnablePersistAuthorization();
    });
}

app.MapRazorPages();
await SeedData.EnsureSeedDataAsync(app.Services);
await app.RunAsync();