using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Background.Cache;
using ServiceDefaults.Interfaces;
using System.Reflection;
using Xunit;

namespace ServiceDefaults.Tests.Background;

public class CacheRefreshBackgroundServiceTests
{
    private readonly Mock<ILogger<CacheRefreshBackgroundService>> _loggerMock = new();

    private static IConfiguration BuildConfig(int? intervalMinutes = null)
    {
        var pairs = intervalMinutes.HasValue
            ? new Dictionary<string, string?> { ["CacheSettings:RefreshIntervalMinutes"] = intervalMinutes.Value.ToString() }
            : new Dictionary<string, string?>();
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }

    private static TimeSpan GetInterval(CacheRefreshBackgroundService service)
    {
        var field = typeof(CacheRefreshBackgroundService)
            .GetField("_interval", BindingFlags.NonPublic | BindingFlags.Instance);
        return (TimeSpan)field!.GetValue(service)!;
    }

    [Fact]
    public void Constructor_DefaultInterval_Is10Minutes()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var service = new CacheRefreshBackgroundService(sp, _loggerMock.Object, BuildConfig());
        GetInterval(service).Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Constructor_CustomInterval_UsesConfigValue()
    {
        var sp = new Mock<IServiceProvider>().Object;
        var service = new CacheRefreshBackgroundService(sp, _loggerMock.Object, BuildConfig(5));
        GetInterval(service).Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ServiceType_InheritsFromBackgroundService()
    {
        typeof(CacheRefreshBackgroundService)
            .IsAssignableTo(typeof(BackgroundService))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenAlreadyCancelled_DoesNotCallPreloaders()
    {
        var preloaderMock = new Mock<ICachePreloader>();
        var service = BuildService(preloaderMock.Object, BuildConfig(0));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var executeMethod = typeof(CacheRefreshBackgroundService)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)executeMethod.Invoke(service, new object[] { cts.Token })!;
        await task;

        preloaderMock.Verify(p => p.PreloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CallsPreloader_OnFirstIteration()
    {
        using var cts = new CancellationTokenSource();

        var preloaderMock = new Mock<ICachePreloader>();
        preloaderMock
            .Setup(p => p.PreloadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .Returns(Task.CompletedTask);

        var service = BuildService(preloaderMock.Object, BuildConfig(0));

        var executeMethod = typeof(CacheRefreshBackgroundService)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)executeMethod.Invoke(service, new object[] { cts.Token })!;

        try { await task; }
        catch (OperationCanceledException) { }

        preloaderMock.Verify(p => p.PreloadAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_PreloaderThrowsException_ServiceContinuesWithoutThrowing()
    {
        using var cts = new CancellationTokenSource();
        var callCount = 0;

        var preloaderMock = new Mock<ICachePreloader>();
        preloaderMock
            .Setup(p => p.PreloadAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Preloader failed");
                cts.Cancel();
                return Task.CompletedTask;
            });

        var service = BuildService(preloaderMock.Object, BuildConfig(0));

        var executeMethod = typeof(CacheRefreshBackgroundService)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)executeMethod.Invoke(service, new object[] { cts.Token })!;

        var act = async () =>
        {
            try { await task; }
            catch (OperationCanceledException) { }
        };

        await act.Should().NotThrowAsync();
        preloaderMock.Verify(p => p.PreloadAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_MultiplePreloaders_AllAreCalled()
    {
        using var cts = new CancellationTokenSource();

        var preloaderA = new Mock<ICachePreloader>();
        var preloaderB = new Mock<ICachePreloader>();
        var callCount = 0;

        preloaderA.Setup(p => p.PreloadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { if (++callCount >= 2) cts.Cancel(); })
            .Returns(Task.CompletedTask);
        preloaderB.Setup(p => p.PreloadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { if (++callCount >= 2) cts.Cancel(); })
            .Returns(Task.CompletedTask);

        var service = BuildService(BuildConfig(0), preloaderA.Object, preloaderB.Object);

        var executeMethod = typeof(CacheRefreshBackgroundService)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)executeMethod.Invoke(service, new object[] { cts.Token })!;

        try { await task; }
        catch (OperationCanceledException) { }

        preloaderA.Verify(p => p.PreloadAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        preloaderB.Verify(p => p.PreloadAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private CacheRefreshBackgroundService BuildService(ICachePreloader preloader, IConfiguration config)
        => BuildService(config, preloader);

    private CacheRefreshBackgroundService BuildService(IConfiguration config, params ICachePreloader[] preloaders)
    {
        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider
            .Setup(p => p.GetService(typeof(IEnumerable<ICachePreloader>)))
            .Returns(preloaders);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var sp = new Mock<IServiceProvider>();
        sp.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);

        return new CacheRefreshBackgroundService(sp.Object, _loggerMock.Object, config);
    }
}
