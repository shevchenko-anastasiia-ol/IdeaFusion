using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Middlewares;

public class GatewayRequestMiddleware
{
    private const string ServiceNameKey = "ServiceName";
    private const string CorrelationHeader = "X-Correlation-Id";
    
    private readonly RequestDelegate _next;
    private readonly long _maxRequestSize;
    private readonly ILogger<GatewayRequestMiddleware> _logger;

    public GatewayRequestMiddleware(
        RequestDelegate next, 
        IConfiguration config,
        ILogger<GatewayRequestMiddleware> logger)
    {
        _next = next;
        _maxRequestSize = config.GetValue<long>("Gateway:MaxRequestSize", 5242880);
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }
        
        var correlationId = context.Items[CorrelationHeader]?.ToString() 
                         ?? context.Items["CorrelationId"]?.ToString() 
                         ?? "unknown";

        // Validate request size
        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxRequestSize)
        {
            _logger.LogWarning(
                "Request payload too large. Size: {Size} bytes, Limit: {Limit} bytes, " +
                "Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
                context.Request.ContentLength.Value, _maxRequestSize,
                context.Request.Path, context.Request.Method, correlationId);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.Headers["X-Request-Size-Limit"] = _maxRequestSize.ToString();
            await context.Response.WriteAsync("Payload too large");
            return;
        }

        // Extract service name from route metadata
        var routeConfig = context.GetEndpoint()?.Metadata.GetMetadata<RouteConfig>();
        var serviceName = routeConfig?.Metadata?.GetValueOrDefault("ServiceName") ?? "unknown-service";

        context.Items[ServiceNameKey] = serviceName;

        _logger.LogDebug(
            "Gateway request metadata. ServiceName: {ServiceName}, Path: {Path}, " +
            "Method: {Method}, CorrelationId: {CorrelationId}",
            serviceName, context.Request.Path, context.Request.Method, correlationId);

        await _next(context);
    }
}