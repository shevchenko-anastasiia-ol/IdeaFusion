using System.Buffers;
using System.Reflection;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using IdeaFusion.Grpc.CollaborationRequests;
using Xunit;

namespace Collaboration.Application.Tests.GrpcService;

// ── Stub that intercepts calls so no real network is needed ───────────────────

file sealed class FakeCallInvoker : CallInvoker
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        if (typeof(TResponse) == typeof(CollaborationRequestResponse))
            return (TResponse)(object)new CollaborationRequestResponse();
        if (typeof(TResponse) == typeof(CollaborationRequestsResponse))
            return (TResponse)(object)new CollaborationRequestsResponse();
        throw new NotSupportedException($"Unexpected response type: {typeof(TResponse).Name}");
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        var response = BlockingUnaryCall(method, host, options, request);
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(Metadata.Empty),
            () => Status.DefaultSuccess,
            () => Metadata.Empty,
            () => { });
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        => throw new NotImplementedException();

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options)
        => throw new NotImplementedException();

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options)
        => throw new NotImplementedException();
}

// ── CollaborationRequestGrpcServiceClient – constructors & NewInstance ────────

// Minimal subclass that exposes the protected parameterless constructor for testing
file sealed class ExposedClient
    : CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient
{
    public ExposedClient() : base() { }   // calls protected CollaborationRequestGrpcServiceClient()
}

public class CollaborationRequestGrpcServiceClientConstructorTests
{
    [Fact]
    public void Constructor_Protected_Parameterless_CreatesClient()
    {
        // Exercises the protected CollaborationRequestGrpcServiceClient() : base() constructor
        var client = new ExposedClient();
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithChannelBase_CreatesClient()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:50051");
        var client = new CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient(channel);
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCallInvoker_CreatesClient()
    {
        var client = new CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient(new FakeCallInvoker());
        client.Should().NotBeNull();
    }

    [Fact]
    public void NewInstance_ViaWithHost_ReturnsNewClientOfSameType()
    {
        var client    = new CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient(new FakeCallInvoker());
        var newClient = client.WithHost("localhost");

        newClient.Should().NotBeNull();
        newClient.Should().BeOfType<CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient>();
        newClient.Should().NotBeSameAs(client);
    }
}

// ── CollaborationRequestGrpcServiceClient – sync wrappers ────────────────────

public class CollaborationRequestGrpcServiceClientSyncTests
{
    private static CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient Client() =>
        new(new FakeCallInvoker());

    [Fact]
    public void GetRequestById_Sync_ReturnsResponse()
    {
        var result = Client().GetRequestById(new GetRequestByIdRequest { RequestId = "r-1" });
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetRequestById_Sync_WithCallOptions_ReturnsResponse()
    {
        var result = Client().GetRequestById(new GetRequestByIdRequest { RequestId = "r-1" }, new CallOptions());
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetRequestsByTeam_Sync_ReturnsResponse()
    {
        var result = Client().GetRequestsByTeam(new GetRequestsByTeamRequest { TeamId = "t-1" });
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetRequestsByTeam_Sync_WithCallOptions_ReturnsResponse()
    {
        var result = Client().GetRequestsByTeam(new GetRequestsByTeamRequest { TeamId = "t-1" }, new CallOptions());
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetRequestsByUser_Sync_ReturnsResponse()
    {
        var result = Client().GetRequestsByUser(new GetRequestsByUserRequest { UserId = "u-1" });
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetRequestsByUser_Sync_WithCallOptions_ReturnsResponse()
    {
        var result = Client().GetRequestsByUser(new GetRequestsByUserRequest { UserId = "u-1" }, new CallOptions());
        result.Should().NotBeNull();
    }
}

// ── CollaborationRequestGrpcServiceClient – async CallOptions overloads ───────

public class CollaborationRequestGrpcServiceClientAsyncTests
{
    private static CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient Client() =>
        new(new FakeCallInvoker());

    [Fact]
    public async Task GetRequestByIdAsync_WithCallOptions_ReturnsResponse()
    {
        var result = await Client().GetRequestByIdAsync(
            new GetRequestByIdRequest { RequestId = "r-1" }, new CallOptions());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRequestsByTeamAsync_WithCallOptions_ReturnsResponse()
    {
        var result = await Client().GetRequestsByTeamAsync(
            new GetRequestsByTeamRequest { TeamId = "t-1" }, new CallOptions());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRequestsByUserAsync_WithCallOptions_ReturnsResponse()
    {
        var result = await Client().GetRequestsByUserAsync(
            new GetRequestsByUserRequest { UserId = "u-1" }, new CallOptions());
        result.Should().NotBeNull();
    }
}

// ── CollaborationRequestGrpcServiceBase – default stubs & BindService ─────────

public class CollaborationRequestGrpcServiceBaseTests
{
    private sealed class StubImpl : CollaborationRequestGrpcService.CollaborationRequestGrpcServiceBase { }

    private sealed class NoOpServiceBinder : ServiceBinderBase
    {
        public int MethodsAdded { get; private set; }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
            => MethodsAdded++;
    }

    // ── BindService ───────────────────────────────────────────────────────────

    [Fact]
    public void BindService_WithImpl_ReturnsNonNullDefinition()
    {
        var def = CollaborationRequestGrpcService.BindService(new StubImpl());
        def.Should().NotBeNull();
    }

    [Fact]
    public void BindService_Binder_WithNonNullImpl_BindsAllThreeMethods()
    {
        var binder = new NoOpServiceBinder();
        CollaborationRequestGrpcService.BindService(binder, new StubImpl());
        binder.MethodsAdded.Should().Be(3);
    }

    [Fact]
    public void BindService_Binder_WithNullImpl_BindsNullHandlers()
    {
        var binder = new NoOpServiceBinder();
        CollaborationRequestGrpcService.BindService(binder, null!);
        binder.MethodsAdded.Should().Be(3);
    }

    // ── Base stubs throw Unimplemented ────────────────────────────────────────

    [Fact]
    public async Task Base_GetRequestById_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestById(new GetRequestByIdRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
                 .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task Base_GetRequestsByTeam_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestsByTeam(new GetRequestsByTeamRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
                 .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task Base_GetRequestsByUser_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestsByUser(new GetRequestsByUserRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
                 .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }
}

// ── Helper types for marshaller reflection tests ──────────────────────────────

file sealed class TestSerializationContext : SerializationContext
{
    private readonly ArrayBufferWriter<byte> _writer = new();
    private byte[]? _completedPayload;

    public override void SetPayloadLength(int payloadLength) { }
    public override IBufferWriter<byte> GetBufferWriter() => _writer;
    public override void Complete() => _completedPayload = _writer.WrittenMemory.ToArray();
    public override void Complete(byte[] payload) => _completedPayload = payload;

    public byte[] GetPayload() => _completedPayload ?? Array.Empty<byte>();
}

file sealed class TestDeserializationContext : DeserializationContext
{
    private readonly byte[] _payload;

    public TestDeserializationContext(byte[] payload) => _payload = payload;

    public override int PayloadLength => _payload.Length;
    public override byte[] PayloadAsNewBuffer() => (byte[])_payload.Clone();
    public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() =>
        new ReadOnlySequence<byte>(_payload);
}

// Implements IMessage but NOT IBufferMessage — forces the else-branch in __Helper_SerializeMessage
file sealed class NonBufferMessage : IMessage
{
    public MessageDescriptor Descriptor => null!;
    public void WriteTo(CodedOutputStream output) { }
    public int CalculateSize() => 0;
    public void MergeFrom(CodedInputStream input) { }
}

// Implements IMessage<T> but NOT IBufferMessage — forces IsBufferMessage=false in __Helper_MessageCache<T>
// and the else-branch of __Helper_DeserializeMessage
file sealed class NonBufferTypedMessage : IMessage<NonBufferTypedMessage>
{
    public static readonly MessageParser<NonBufferTypedMessage> Parser =
        new MessageParser<NonBufferTypedMessage>(() => new NonBufferTypedMessage());

    public MessageDescriptor Descriptor => null!;
    public void WriteTo(CodedOutputStream output) { }
    public int CalculateSize() => 0;
    public void MergeFrom(CodedInputStream input) { }
    public void MergeFrom(NonBufferTypedMessage message) { }
    public NonBufferTypedMessage Clone() => new();
    public bool Equals(NonBufferTypedMessage? other) => other is not null;
}

// ── __Helper_SerializeMessage / __Helper_DeserializeMessage / __Helper_MessageCache ──

public class CollaborationRequestGrpcServiceMarshallerCoverageTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;

    private static Marshaller<T> GetMarshaller<T>(string fieldName) =>
        (Marshaller<T>)typeof(CollaborationRequestGrpcService)
            .GetField(fieldName, Private)!
            .GetValue(null)!;

    private static MethodInfo DeserializeMethod =>
        typeof(CollaborationRequestGrpcService).GetMethod("__Helper_DeserializeMessage", Private)!;

    private static MethodInfo SerializeMethod =>
        typeof(CollaborationRequestGrpcService).GetMethod("__Helper_SerializeMessage", Private)!;

    // ── __Helper_SerializeMessage: IBufferMessage path (true branch) ─────────

    [Fact]
    public void SerializeMessage_WithIBufferMessage_UsesBufferWriterAndComplete()
    {
        var marshaller = GetMarshaller<GetRequestByIdRequest>(
            "__Marshaller_collaboration_requests_GetRequestByIdRequest");
        var ctx = new TestSerializationContext();

        marshaller.ContextualSerializer(new GetRequestByIdRequest { RequestId = "r-1" }, ctx);

        ctx.GetPayload().Should().NotBeNull();
    }

    [Fact]
    public void SerializeMessage_WithIBufferMessage_ForTeamRequest_UsesBufferWriter()
    {
        var marshaller = GetMarshaller<GetRequestsByTeamRequest>(
            "__Marshaller_collaboration_requests_GetRequestsByTeamRequest");
        var ctx = new TestSerializationContext();

        marshaller.ContextualSerializer(new GetRequestsByTeamRequest { TeamId = "t-1" }, ctx);

        ctx.GetPayload().Should().NotBeNull();
    }

    // ── __Helper_SerializeMessage: non-IBufferMessage path (false branch, line 27) ──

    [Fact]
    public void SerializeMessage_WithNonIBufferMessage_UsesToByteArrayFallback()
    {
        var ctx = new TestSerializationContext();

        // NonBufferMessage does NOT implement IBufferMessage → takes the else branch
        SerializeMethod.Invoke(null, new object[] { new NonBufferMessage(), ctx });

        ctx.GetPayload().Should().NotBeNull();
    }

    // ── __Helper_MessageCache<T> + __Helper_DeserializeMessage: IsBufferMessage=true path ──

    [Fact]
    public void DeserializeMessage_WithProtobufType_UsesReadOnlySequencePath()
    {
        var msgBytes = new GetRequestByIdRequest { RequestId = "r-42" }.ToByteArray();
        var ctx      = new TestDeserializationContext(msgBytes);

        var result = (GetRequestByIdRequest)DeserializeMethod
            .MakeGenericMethod(typeof(GetRequestByIdRequest))
            .Invoke(null, new object[] { ctx, GetRequestByIdRequest.Parser })!;

        result.RequestId.Should().Be("r-42");
    }

    [Fact]
    public void DeserializeMessage_CollaborationRequestResponse_UsesReadOnlySequencePath()
    {
        var original = new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto { RequestId = "req-1", Status = "Pending" }
        };
        var ctx = new TestDeserializationContext(original.ToByteArray());

        var result = (CollaborationRequestResponse)DeserializeMethod
            .MakeGenericMethod(typeof(CollaborationRequestResponse))
            .Invoke(null, new object[] { ctx, CollaborationRequestResponse.Parser })!;

        result.Request!.RequestId.Should().Be("req-1");
    }

    // ── __Helper_DeserializeMessage: IsBufferMessage=false path (line 45) ────

    [Fact]
    public void DeserializeMessage_WithNonIBufferType_UsesPayloadAsNewBufferFallback()
    {
        // NonBufferTypedMessage does NOT implement IBufferMessage
        // → IsBufferMessage = false → parser.ParseFrom(context.PayloadAsNewBuffer()) is called
        var ctx = new TestDeserializationContext(Array.Empty<byte>());

        var result = (NonBufferTypedMessage)DeserializeMethod
            .MakeGenericMethod(typeof(NonBufferTypedMessage))
            .Invoke(null, new object[] { ctx, NonBufferTypedMessage.Parser })!;

        result.Should().NotBeNull();
    }
}
