using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AggregatorService.HealthChecks;

public class ContentServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContentServiceHealthCheck> _logger;

    public ContentServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<ContentServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ContentClient");
            var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("ContentService is reachable.")
                : HealthCheckResult.Unhealthy($"ContentService returned {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ContentService health check failed.");
            return HealthCheckResult.Unhealthy("ContentService is unreachable.", ex);
        }
    }
}