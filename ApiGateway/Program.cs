using ApiGateway.Middlewares;
using ApiGateway.Transforms;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;


var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults for unified logging, tracing, and service discovery
builder.AddServiceDefaults();

// Configure YARP reverse proxy with service discovery
// This enables YARP to resolve service names (e.g., "http://catalogservice") to actual service URLs
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver(); // Enable service discovery for YARP destination resolution

builder.Services.AddSingleton<ITransformProvider, CorrelationIdTransformProvider>();




var app = builder.Build();

// Configure middleware pipeline
// CorrelationId middleware is automatically added by MapDefaultEndpoints()
app.MapDefaultEndpoints(); // Adds /health and /alive endpoints with ServiceDefaults

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();