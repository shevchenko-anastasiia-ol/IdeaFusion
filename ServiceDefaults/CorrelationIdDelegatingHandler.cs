using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DelegatingHandler that automatically adds CorrelationId to outgoing HTTP requests.
/// This ensures CorrelationId is propagated through all microservices in the call chain.
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorrelationIdDelegatingHandler> _logger;

    public CorrelationIdDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CorrelationIdDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get CorrelationId from current HttpContext (set by CorrelationIdMiddleware)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var correlationId = httpContext.Items[CorrelationIdHeader]?.ToString() 
                             ?? httpContext.Items["CorrelationId"]?.ToString();

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                // Add CorrelationId to outgoing request header
                request.Headers.Add(CorrelationIdHeader, correlationId);

                // Also add to Activity tags for OpenTelemetry tracing
                Activity.Current?.SetTag("correlation.id", correlationId);
                Activity.Current?.SetTag("http.request.header.X-Correlation-Id", correlationId);

                _logger.LogDebug(
                    "Added CorrelationId to outgoing HTTP request. Uri: {Uri}, CorrelationId: {CorrelationId}",
                    request.RequestUri,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "CorrelationId not found in HttpContext for outgoing request. Uri: {Uri}",
                    request.RequestUri);
            }
        }
        else
        {
            _logger.LogWarning(
                "HttpContext is null for outgoing request. CorrelationId cannot be propagated. Uri: {Uri}",
                request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

