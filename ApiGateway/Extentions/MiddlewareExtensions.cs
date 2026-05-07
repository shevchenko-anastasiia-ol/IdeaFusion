using ApiGateway.Middlewares;
using ServiceDefaults.Extensions;

namespace ApiGateway.Extentions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayPipeline(this IApplicationBuilder app)
    {
        app.UseCorrelationId();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<GatewayLoggingMiddleware>();
        app.UseMiddleware<GatewayRequestMiddleware>();
        app.UseMiddleware<TimeoutMiddleware>();
        app.UseMiddleware<YarpProxyLoggingMiddleware>();
        return app;
    }
}