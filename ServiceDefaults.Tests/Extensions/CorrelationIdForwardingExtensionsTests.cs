using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Extensions;
using ServiceDefaults.Http;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class CorrelationIdForwardingExtensionsTests
{
    [Fact]
    public void AddCorrelationIdForwarding_RegistersIHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddCorrelationIdForwarding();

        services.Should().Contain(sd => sd.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddCorrelationIdForwarding_RegistersCorrelationIdHandler()
    {
        var services = new ServiceCollection();
        services.AddCorrelationIdForwarding();

        services.Should().Contain(sd => sd.ServiceType == typeof(CorrelationIdHandler));
    }

    [Fact]
    public void AddCorrelationIdForwarding_CorrelationIdHandler_IsTransient()
    {
        var services = new ServiceCollection();
        services.AddCorrelationIdForwarding();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(CorrelationIdHandler) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddCorrelationIdForwarding_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result   = services.AddCorrelationIdForwarding();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCorrelationIdHttpClient_ByName_ReturnsNonNullBuilder()
    {
        var services = new ServiceCollection();
        var result   = services.AddCorrelationIdHttpClient("test-client", _ => { });
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddCorrelationIdHttpClient_Generic_ReturnsNonNullBuilder()
    {
        var services = new ServiceCollection();
        var result   = services.AddCorrelationIdHttpClient<TestTypedClient>(_ => { });
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddCorrelationIdHttpClient_ByName_RegistersNamedHttpClient()
    {
        var services = new ServiceCollection();
        services.AddCorrelationIdHttpClient("my-client", _ => { });

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHttpClientFactory));
    }

    private class TestTypedClient { }
}
