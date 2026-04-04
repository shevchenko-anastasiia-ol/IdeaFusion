using AggregatorService.Clients;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults for unified logging, tracing, and service discovery
builder.AddServiceDefaults();

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

// Register typed HttpClients with Aspire service discovery
// Service names match the logical names defined in AppHost
// CorrelationId is automatically propagated via CorrelationIdDelegatingHandler from ServiceDefaults

// ContentService client
// Aspire automatically resolves "http://contentservice" to the actual service URL
builder.Services.AddHttpClient<ContentClient>(client =>
{
    client.BaseAddress = new Uri("http://contentservice");
});

// CollaborationService client
// Aspire automatically resolves "http://collaborationservice" to the actual service URL
builder.Services.AddHttpClient<CollaborationClient>(client =>
{
    client.BaseAddress = new Uri("http://collaborationservice");
});

var app = builder.Build();

// CorrelationId middleware is automatically added by MapDefaultEndpoints()
app.MapDefaultEndpoints(); // Adds /health and /alive endpoints with ServiceDefaults

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aggregator API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();

app.Run();