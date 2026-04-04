using System.Diagnostics;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Middlewares;

public class GatewayLoggingMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;

    public GatewayLoggingMiddleware(RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Items[CorrelationHeader]?.ToString() 
                         ?? context.Items["CorrelationId"]?.ToString() 
                         ?? "unknown";
        
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (clientIp == "::1") clientIp = "127.0.0.1";

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var queryString = context.Request.QueryString.Value ?? string.Empty;
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var requestSize = context.Request.ContentLength ?? 0;

        // Log incoming request with structured properties
        _logger.LogInformation(
            "Gateway request started. Method: {Method}, Path: {Path}, QueryString: {QueryString}, " +
            "ClientIp: {ClientIp}, UserAgent: {UserAgent}, RequestSize: {RequestSize} bytes, CorrelationId: {CorrelationId}",
            method, path, queryString, clientIp, userAgent, requestSize, correlationId);

        try
        {
            await _next(context);
            stopwatch.Stop();

            var responseSize = context.Response.ContentLength ?? 0;
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Get routing information if available
            var endpoint = context.GetEndpoint();
            var routeConfig = endpoint?.Metadata.GetMetadata<RouteConfig>();
            var routeId = routeConfig?.RouteId ?? "no-route";
            var clusterId = routeConfig?.ClusterId ?? "no-cluster";
            var serviceName = routeConfig?.Metadata?.GetValueOrDefault("ServiceName") ?? "unknown";

            // Log routing decision
            if (routeConfig != null)
            {
                _logger.LogInformation(
                    "Routing decision. RouteId: {RouteId}, ClusterId: {ClusterId}, ServiceName: {ServiceName}, " +
                    "Path: {Path}, CorrelationId: {CorrelationId}",
                    routeId, clusterId, serviceName, path, correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "No route matched. Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
                    path, method, correlationId);
            }

            // Log response with performance metrics
            _logger.LogInformation(
                "Gateway response. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, " +
                "ElapsedMs: {ElapsedMs}, ResponseSize: {ResponseSize} bytes, RouteId: {RouteId}, " +
                "ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                method, path, statusCode, elapsedMs, responseSize, routeId, clusterId, correlationId);

            // Log slow requests as warning
            if (elapsedMs > 3000)
            {
                _logger.LogWarning(
                    "Slow request detected. Method: {Method}, Path: {Path}, ElapsedMs: {ElapsedMs}, " +
                    "RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                    method, path, elapsedMs, routeId, clusterId, correlationId);
            }

            // Log error responses
            if (statusCode >= 500)
            {
                _logger.LogError(
                    "Server error response. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, " +
                    "ElapsedMs: {ElapsedMs}, RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                    method, path, statusCode, elapsedMs, routeId, clusterId, correlationId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "Client error response. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, " +
                    "ElapsedMs: {ElapsedMs}, RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                    method, path, statusCode, elapsedMs, routeId, clusterId, correlationId);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var routeConfig = context.GetEndpoint()?.Metadata.GetMetadata<RouteConfig>();
            
            _logger.LogError(ex,
                "Gateway error. Method: {Method}, Path: {Path}, ElapsedMs: {ElapsedMs}, " +
                "RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}, " +
                "ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                method, path, stopwatch.ElapsedMilliseconds,
                routeConfig?.RouteId ?? "unknown", routeConfig?.ClusterId ?? "unknown",
                correlationId, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}