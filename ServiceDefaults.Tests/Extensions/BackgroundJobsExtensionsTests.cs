using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceDefaults.Background.Cache;
using ServiceDefaults.Extensions;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class BackgroundJobsExtensionsTests
{
    [Fact]
    public void AddCacheBackgroundJobs_RegistersCacheRefreshBackgroundService_AsHostedService()
    {
        var services = new ServiceCollection();
        services.AddCacheBackgroundJobs();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType == typeof(CacheRefreshBackgroundService));
    }

    [Fact]
    public void AddCacheBackgroundJobs_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddCacheBackgroundJobs();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCacheBackgroundJobs_RegisteredAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCacheBackgroundJobs();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType == typeof(CacheRefreshBackgroundService) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }
}
