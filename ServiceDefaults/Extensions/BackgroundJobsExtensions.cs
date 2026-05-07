using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Background.Cache;

namespace ServiceDefaults.Extensions;

public static class BackgroundJobsExtensions
{
    public static IServiceCollection AddCacheBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<CacheRefreshBackgroundService>();
        return services;
    }
}