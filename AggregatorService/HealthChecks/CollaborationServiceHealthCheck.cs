using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AggregatorService.HealthChecks;

public class CollaborationServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CollaborationServiceHealthCheck> _logger;

    public CollaborationServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<CollaborationServiceHealthCheck> logger)
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
            var client = _httpClientFactory.CreateClient("CollaborationClient");
            var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("CollaborationService is reachable.")
                : HealthCheckResult.Unhealthy($"CollaborationService returned {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CollaborationService health check failed.");
            return HealthCheckResult.Unhealthy("CollaborationService is unreachable.", ex);
        }
    }
}