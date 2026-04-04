using Microsoft.Extensions.Caching.Memory;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var routeConfig = endpoint?.Metadata.GetMetadata<RouteConfig>();
        if (routeConfig == null)
        {
            await _next(context);
            return;
        }

        int requestsPerMinute = GetIntMetadata(routeConfig, "RateLimit:RequestsPerMinute")
                                ?? _config.GetValue("Gateway:DefaultRateLimit:RequestsPerMinute", 100);

        int burst = GetIntMetadata(routeConfig, "RateLimit:Burst")
                    ?? _config.GetValue("Gateway:DefaultRateLimit:Burst", 20);

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "shared";
        var routeId = routeConfig.RouteId ?? "unknown-route";
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "unknown";
        var cacheKey = $"rate:{routeId}:{clientIp}";

        var now = DateTime.UtcNow;
        if (!_cache.TryGetValue(cacheKey, out RateLimitCounter? counter))
        {
            counter = new RateLimitCounter { Count = 0, Timestamp = now };
        }

        if (now - counter!.Timestamp > TimeSpan.FromMinutes(1))
        {
            counter.Count = 0;
            counter.Timestamp = now;
        }

        counter.Count++;
        _cache.Set(cacheKey, counter, TimeSpan.FromMinutes(2));

        if (counter.Count > requestsPerMinute + burst)
        {
            _logger.LogWarning(
                "Rate limit exceeded. ClientIp: {ClientIp}, RouteId: {RouteId}, CorrelationId: {CorrelationId}, " +
                "Count: {Count}, Limit: {Limit}, Burst: {Burst}, Method: {Method}, Path: {Path}",
                clientIp, routeId, correlationId, counter.Count, requestsPerMinute, burst,
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            context.Response.Headers["X-RateLimit-Limit"] = requestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = requestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] =
            Math.Max(0, requestsPerMinute + burst - counter.Count).ToString();

        await _next(context);
    }

    private static int? GetIntMetadata(RouteConfig routeConfig, string key)
    {
        if (routeConfig.Metadata != null &&
            routeConfig.Metadata.TryGetValue(key, out var val) &&
            int.TryParse(val, out var result))
        {
            return result;
        }

        return null;
    }

    private sealed class RateLimitCounter
    {
        public int Count;
        public DateTime Timestamp;
    }
}