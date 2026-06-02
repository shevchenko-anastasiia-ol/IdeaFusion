using FluentAssertions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceDefaults.Grpc.Interceptors;
using Xunit;

namespace ServiceDefaults.Tests.Grpc;

public class GrpcLoggingInterceptorTests
{
    private readonly Mock<ILogger<GrpcLoggingInterceptor>> _loggerMock = new();

    private GrpcLoggingInterceptor Create() => new(_loggerMock.Object);

    // ── structural ────────────────────────────────────────────────────────────

    [Fact]
    public void GrpcLoggingInterceptor_InheritsFromInterceptor()
    {
        typeof(GrpcLoggingInterceptor).IsAssignableTo(typeof(Interceptor)).Should().BeTrue();
    }

    // ── UnaryServerHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UnaryServerHandler_OnSuccess_ReturnsResponseFromContinuation()
    {
        var interceptor = Create();
        var context     = new TestServerCallContext("/Test/Method", "client-peer");

        UnaryServerMethod<string, string> continuation = (_, _) => Task.FromResult("the-response");

        var result = await interceptor.UnaryServerHandler("request", context, continuation);

        result.Should().Be("the-response");
    }

    [Fact]
    public async Task UnaryServerHandler_OnSuccess_CallsContinuationWithOriginalRequest()
    {
        var interceptor    = Create();
        var context        = new TestServerCallContext();
        string? captured   = null;

        UnaryServerMethod<string, string> continuation = (req, _) =>
        {
            captured = req;
            return Task.FromResult("ok");
        };

        await interceptor.UnaryServerHandler("my-request", context, continuation);

        captured.Should().Be("my-request");
    }

    [Fact]
    public async Task UnaryServerHandler_OnRpcException_RethrowsRpcException()
    {
        var interceptor = Create();
        var context     = new TestServerCallContext();
        var rpcEx       = new RpcException(new Status(StatusCode.NotFound, "not found"));

        UnaryServerMethod<string, string> continuation = (_, _) => throw rpcEx;

        var act = () => interceptor.UnaryServerHandler("req", context, continuation);

        await act.Should().ThrowAsync<RpcException>()
                 .Where(e => e.StatusCode == StatusCode.NotFound);
    }

    [Fact]
    public async Task UnaryServerHandler_OnRpcException_LogsError()
    {
        var interceptor = Create();
        var context     = new TestServerCallContext();
        var rpcEx       = new RpcException(new Status(StatusCode.Internal, "internal error"));

        UnaryServerMethod<string, string> continuation = (_, _) => throw rpcEx;

        try { await interceptor.UnaryServerHandler("req", context, continuation); }
        catch (RpcException) { }

        _loggerMock.Invocations.Should().Contain(i =>
            i.Method.Name == nameof(ILogger.Log));
    }

    [Fact]
    public async Task UnaryServerHandler_OnGenericException_RethrowsOriginalException()
    {
        var interceptor = Create();
        var context     = new TestServerCallContext();

        UnaryServerMethod<string, string> continuation = (_, _) =>
            throw new InvalidOperationException("unexpected");

        var act = () => interceptor.UnaryServerHandler("req", context, continuation);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("unexpected");
    }

    [Fact]
    public async Task UnaryServerHandler_OnGenericException_LogsError()
    {
        var interceptor = Create();
        var context     = new TestServerCallContext();

        UnaryServerMethod<string, string> continuation = (_, _) =>
            throw new InvalidOperationException("boom");

        try { await interceptor.UnaryServerHandler("req", context, continuation); }
        catch (InvalidOperationException) { }

        _loggerMock.Invocations.Should().Contain(i =>
            i.Method.Name == nameof(ILogger.Log));
    }
}

// ── test helper ───────────────────────────────────────────────────────────────

internal sealed class TestServerCallContext : ServerCallContext
{
    private readonly string _method;
    private readonly string _peer;

    public TestServerCallContext(string method = "/TestService/TestMethod", string peer = "test-peer")
    {
        _method = method;
        _peer   = peer;
    }

    protected override string MethodCore              => _method;
    protected override string HostCore                => "localhost";
    protected override string PeerCore                => _peer;
    protected override DateTime DeadlineCore          => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore    => new();
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore  => new();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore     => throw new NotImplementedException();

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        => throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        => Task.CompletedTask;
}
