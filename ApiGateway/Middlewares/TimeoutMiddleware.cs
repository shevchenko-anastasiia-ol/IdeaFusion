using System.Diagnostics;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Middlewares;

public class TimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TimeoutMiddleware> _logger;

        public TimeoutMiddleware(
            RequestDelegate next, 
            IConfiguration configuration,
            ILogger<TimeoutMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
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

            var clusterId = routeConfig.ClusterId ?? "unknown";
            var timeoutKey = $"ReverseProxy:Clusters:{clusterId}:HttpRequest:ActivityTimeout";

            var timeoutValue = _configuration[timeoutKey];
            if (string.IsNullOrEmpty(timeoutValue))
            {
                await _next(context);
                return;
            }
            
            var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "unknown";

            if (!TimeSpan.TryParse(timeoutValue, System.Globalization.CultureInfo.InvariantCulture, out var timeout))
            {
                _logger.LogWarning(
                    "Invalid timeout value. TimeoutValue: {TimeoutValue}, ClusterId: {ClusterId}, CorrelationId: {CorrelationId}",
                    timeoutValue, clusterId, correlationId);
                await _next(context);
                return;
            }

            
            var sw = Stopwatch.StartNew();

            using var cts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, cts.Token);

            context.RequestAborted = linkedCts.Token;

            try
            {
                await _next(context);
                sw.Stop();

                _logger.LogDebug(
                    "Request completed. ClusterId: {ClusterId}, Method: {Method}, Path: {Path}, " +
                    "ElapsedMs: {ElapsedMs}, Timeout: {Timeout}ms, CorrelationId: {CorrelationId}",
                    clusterId, context.Request.Method, context.Request.Path,
                    sw.ElapsedMilliseconds, timeout.TotalMilliseconds, correlationId);
            }
            catch (OperationCanceledException ex)
            {
                sw.Stop();
                if (cts.IsCancellationRequested)
                {
                    _logger.LogWarning(ex,
                        "Request timed out. TimeoutSeconds: {TimeoutSeconds}, ClusterId: {ClusterId}, " +
                        "Method: {Method}, Path: {Path}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                        timeout.TotalSeconds, clusterId, context.Request.Method, context.Request.Path,
                        sw.ElapsedMilliseconds, correlationId);

                    context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                    await context.Response.WriteAsync("Gateway Timeout");
                }
                else
                {
                    _logger.LogError(ex,
                        "Request aborted externally. ClusterId: {ClusterId}, Method: {Method}, Path: {Path}, " +
                        "ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                        clusterId, context.Request.Method, context.Request.Path,
                        sw.ElapsedMilliseconds, correlationId);
                }
            }
        }
    }