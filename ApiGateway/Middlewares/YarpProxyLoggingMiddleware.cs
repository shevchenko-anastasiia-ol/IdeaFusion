using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace ApiGateway.Middlewares;

public class YarpProxyLoggingMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<YarpProxyLoggingMiddleware> _logger;

    public YarpProxyLoggingMiddleware(RequestDelegate next, ILogger<YarpProxyLoggingMiddleware> logger)
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
        var correlationId = context.Items[CorrelationHeader]?.ToString() 
                         ?? context.Items["CorrelationId"]?.ToString() 
                         ?? "unknown";

        var endpoint = context.GetEndpoint();
        var routeConfig = endpoint?.Metadata.GetMetadata<RouteConfig>();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        if (routeConfig != null)
        {
            var serviceName = routeConfig.Metadata?.GetValueOrDefault("ServiceName") ?? "unknown";
            _logger.LogInformation(
                "YARP proxy forwarding. RouteId: {RouteId}, ClusterId: {ClusterId}, ServiceName: {ServiceName}, " +
                "Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                routeConfig.RouteId, routeConfig.ClusterId, serviceName, method, path, correlationId);
        }
        else
        {
            _logger.LogWarning(
                "YARP proxy: No route matched. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                method, path, correlationId);
        }

        await _next(context);

        var errorFeature = context.Features.Get<IForwarderErrorFeature>();

        if (errorFeature != null)
        {
            var errorType = errorFeature.Error.ToString();
            var exception = errorFeature.Exception;
            var destination = "unknown";
            
            // Log detailed error information for unavailable services
            _logger.LogError(exception,
                "YARP proxy forwarding failed. Error: {Error}, ErrorType: {ErrorType}, " +
                "RouteId: {RouteId}, ClusterId: {ClusterId}, Destination: {Destination}, " +
                "Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, " +
                "ExceptionType: {ExceptionType}, ExceptionMessage: {ExceptionMessage}",
                errorFeature.Error, errorType, routeConfig?.RouteId ?? "unknown",
                routeConfig?.ClusterId ?? "unknown", destination, method, path, correlationId,
                exception?.GetType().Name ?? "none", exception?.Message ?? "none");

            // Log specific error types for better troubleshooting
            if (exception != null)
            {
                if (exception is System.Net.Http.HttpRequestException httpEx)
                {
                    _logger.LogError(
                        "HTTP request exception details. InnerException: {InnerException}, " +
                        "RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                        httpEx.InnerException?.Message ?? "none", routeConfig?.RouteId, 
                        routeConfig?.ClusterId, correlationId);
                }
                else if (exception is System.Net.Sockets.SocketException socketEx)
                {
                    _logger.LogError(
                        "Socket exception. SocketErrorCode: {SocketErrorCode}, " +
                        "RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                        socketEx.SocketErrorCode, routeConfig?.RouteId, routeConfig?.ClusterId, correlationId);
                }
                else if (exception is System.Threading.Tasks.TaskCanceledException)
                {
                    _logger.LogError(
                        "Request timeout. RouteId: {RouteId}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                        routeConfig?.RouteId, routeConfig?.ClusterId, correlationId);
                }
            }
        }
        else if (routeConfig != null)
        {
            _logger.LogDebug(
                "YARP proxy forwarding succeeded. RouteId: {RouteId}, ClusterId: {ClusterId}, " +
                "StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                routeConfig.RouteId, routeConfig.ClusterId, context.Response.StatusCode,
                method, path, correlationId);
        }
    }
}