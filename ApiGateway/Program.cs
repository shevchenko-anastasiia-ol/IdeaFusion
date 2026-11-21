using ApiGateway.Extentions;
using ApiGateway.Middlewares;
using ServiceDefaults.Extensions;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Services.AddMemoryCache();
builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();
builder.Services.AddServiceDiscovery();

builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddIdeaFusionAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var correlationId = transformContext.HttpContext.Items["X-Correlation-Id"]?.ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                    "X-Correlation-Id", correlationId);
            }

            // YARP already forwards the Authorization header; adding it again sent a doubled
            // value downstream and every service rejected the token (401 invalid_token).

            await ValueTask.CompletedTask;
        });
    });

builder.Services.AddHealthChecks();

builder.Services.AddCors(options => {
    options.AddPolicy("Frontend", policy => {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseGatewayPipeline();
app.UseCorrelationId();
app.UseCors("Frontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapHealthChecks("/health").AllowAnonymous();
await app.RunAsync();   