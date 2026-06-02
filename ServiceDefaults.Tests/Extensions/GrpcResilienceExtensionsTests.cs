using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Extensions;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class GrpcResilienceExtensionsTests
{
    // ── ResilienceProfile enum ────────────────────────────────────────────────

    [Fact]
    public void ResilienceProfile_HasFourValues()
    {
        Enum.GetValues<ResilienceProfile>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(ResilienceProfile.Critical)]
    [InlineData(ResilienceProfile.Standard)]
    [InlineData(ResilienceProfile.ReadOptimized)]
    [InlineData(ResilienceProfile.WriteOptimized)]
    public void ResilienceProfile_AllValuesAreDefined(ResilienceProfile profile)
    {
        Enum.IsDefined(profile).Should().BeTrue();
    }

    // ── AddGrpcResilienceHandler (params) ────────────────────────────────────

    [Fact]
    public void AddGrpcResilienceHandler_WithDefaultParams_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder  = services.AddHttpClient("test");

        var result = builder.AddGrpcResilienceHandler();

        result.Should().NotBeNull();
    }

    [Fact]
    public void AddGrpcResilienceHandler_WithCustomParams_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var builder  = services.AddHttpClient("test");

        var act = () => builder.AddGrpcResilienceHandler(
            maxRetryAttempts: 5,
            delaySeconds: 2,
            failureRatio: 0.4,
            minimumThroughput: 20,
            breakDurationSeconds: 45,
            samplingDurationSeconds: 60,
            timeoutSeconds: 20);

        act.Should().NotThrow();
    }

    // ── AddGrpcResilienceHandler (profile) ───────────────────────────────────

    [Theory]
    [InlineData(ResilienceProfile.Critical)]
    [InlineData(ResilienceProfile.Standard)]
    [InlineData(ResilienceProfile.ReadOptimized)]
    [InlineData(ResilienceProfile.WriteOptimized)]
    public void AddGrpcResilienceHandler_WithProfile_DoesNotThrow(ResilienceProfile profile)
    {
        var services = new ServiceCollection();
        var builder  = services.AddHttpClient("test");

        var act = () => builder.AddGrpcResilienceHandler(profile);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(ResilienceProfile.Critical)]
    [InlineData(ResilienceProfile.Standard)]
    [InlineData(ResilienceProfile.ReadOptimized)]
    [InlineData(ResilienceProfile.WriteOptimized)]
    public void AddGrpcResilienceHandler_WithProfile_ReturnsNonNullBuilder(ResilienceProfile profile)
    {
        var services = new ServiceCollection();
        var builder  = services.AddHttpClient("test");

        var result = builder.AddGrpcResilienceHandler(profile);

        result.Should().NotBeNull();
    }
}
