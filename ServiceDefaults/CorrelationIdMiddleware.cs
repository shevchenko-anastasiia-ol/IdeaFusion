using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Middleware that generates and propagates CorrelationId across the request chain.
/// CorrelationId is a unique identifier for tracking requests across microservices.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CorrelationIdItemKey = "CorrelationId";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for existing CorrelationId in request header
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // Generate new GUID if header is missing (first service in the chain)
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            _logger.LogDebug("Generated new CorrelationId: {CorrelationId}", correlationId);
        }

        // Store CorrelationId in HttpContext.Items for use in the request pipeline
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Items[CorrelationIdHeader] = correlationId;

        // Add CorrelationId to response headers so client can use it for support requests
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push CorrelationId to Serilog LogContext for automatic inclusion in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Log request start
            _logger.LogInformation(
                "Request started. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log request completion
                _logger.LogInformation(
                    "Request completed. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request failed. Method: {Method}, Path: {Path}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
                throw;
            }
        }
    }
}

/// <summary>
/// Extension methods for adding CorrelationId middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds CorrelationId middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}

