using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Health;
using Xunit;

namespace ServiceDefaults.Tests.Health;

public class GrpcServiceHealthCheckTests
{
    private readonly Mock<ILogger<GrpcServiceHealthCheck<TestGrpcClient>>> _loggerMock = new();

    private TestGrpcHealthCheck CreateCheck(Func<CancellationToken, Task<bool>> perform) =>
        new(new TestGrpcClient(), _loggerMock.Object, perform);

    private static HealthCheckContext MakeContext(IHealthCheck check) => new()
    {
        Registration = new HealthCheckRegistration("test", check, null, null)
    };

    // ── structural ────────────────────────────────────────────────────────────

    [Fact]
    public void GrpcServiceHealthCheck_ImplementsIHealthCheck()
    {
        typeof(GrpcServiceHealthCheck<TestGrpcClient>)
            .IsAssignableTo(typeof(IHealthCheck))
            .Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var act = () => new TestGrpcHealthCheck(null!, _loggerMock.Object, _ => Task.FromResult(true));
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new TestGrpcHealthCheck(new TestGrpcClient(), null!, _ => Task.FromResult(true));
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ── healthy / unhealthy from PerformHealthCheckAsync ─────────────────────

    [Fact]
    public async Task CheckHealthAsync_WhenPerformReturnsTrue_ReturnsHealthy()
    {
        var check  = CreateCheck(_ => Task.FromResult(true));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPerformReturnsFalse_ReturnsUnhealthy()
    {
        var check  = CreateCheck(_ => Task.FromResult(false));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPerformReturnsTrue_DescriptionContainsServiceName()
    {
        var check  = CreateCheck(_ => Task.FromResult(true));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Description.Should().Contain("TestService");
    }

    // ── exception handling ────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_RpcExceptionUnavailable_ReturnsUnhealthy()
    {
        var check  = CreateCheck(_ => throw new RpcException(new Status(StatusCode.Unavailable, "down")));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_RpcExceptionDeadlineExceeded_ReturnsUnhealthy()
    {
        var check  = CreateCheck(_ => throw new RpcException(new Status(StatusCode.DeadlineExceeded, "timeout")));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OperationCancelled_ReturnsUnhealthy()
    {
        var check  = CreateCheck(_ => throw new OperationCanceledException());
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_GenericException_ReturnsUnhealthy()
    {
        var check  = CreateCheck(_ => throw new InvalidOperationException("unexpected"));
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_RpcExceptionUnavailable_ExceptionAttachedToResult()
    {
        var rpcEx  = new RpcException(new Status(StatusCode.Unavailable, "service down"));
        var check  = CreateCheck(_ => throw rpcEx);
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Exception.Should().Be(rpcEx);
    }

    [Fact]
    public async Task CheckHealthAsync_GenericException_ExceptionAttachedToResult()
    {
        var ex     = new Exception("boom");
        var check  = CreateCheck(_ => throw ex);
        var result = await check.CheckHealthAsync(MakeContext(check));
        result.Exception.Should().Be(ex);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    public class TestGrpcClient { }

    public sealed class TestGrpcHealthCheck : GrpcServiceHealthCheck<TestGrpcClient>
    {
        private readonly Func<CancellationToken, Task<bool>> _perform;

        public TestGrpcHealthCheck(
            TestGrpcClient client,
            ILogger<GrpcServiceHealthCheck<TestGrpcClient>> logger,
            Func<CancellationToken, Task<bool>> perform)
            : base(client, logger)
        {
            _perform = perform;
        }

        protected override string ServiceName => "TestService";

        protected override Task<bool> PerformHealthCheckAsync(CancellationToken cancellationToken)
            => _perform(cancellationToken);
    }
}
