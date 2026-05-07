using AggregatorService.Clients;
using System.Text.Json;
using System.Text.Json.Serialization;
using AggregatorService.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();
builder.Services.AddServiceDiscovery();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("ContentClient", client =>
{
    client.BaseAddress = new Uri("http://contentservice");
}).AddServiceDiscovery();

builder.Services.AddHttpClient("CollaborationClient", client =>
{
    client.BaseAddress = new Uri("http://collaborationservice");
}).AddServiceDiscovery();

builder.Services.AddHttpClient<ContentClient>(client =>
{
    client.BaseAddress = new Uri("http://contentservice");
}).AddServiceDiscovery();

builder.Services.AddHttpClient<CollaborationClient>(client =>
{
    client.BaseAddress = new Uri("http://collaborationservice");
}).AddServiceDiscovery();

builder.Services.AddHealthChecks()
    .AddCheck<ContentServiceHealthCheck>(
        "content-http",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["http", "downstream", "ready"])
    .AddCheck<CollaborationServiceHealthCheck>(
        "collaboration-http",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["http", "downstream", "ready"]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aggregator API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();