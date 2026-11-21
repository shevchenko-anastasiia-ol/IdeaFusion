using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.Grpc.Posts;
using IdeaFusion.Grpc.Teams;
using IdeaFusion.Grpc.Users;
using Moq;
using System.Reflection;
using Xunit;

namespace GrpcClients.Tests.Proto;

// ═══════════════════════════════════════════════════════════════════════════
// Shared helpers
// ═══════════════════════════════════════════════════════════════════════════

file static class FakeCall
{
    public static AsyncUnaryCall<T> Of<T>(T result) =>
        new(Task.FromResult(result),
            Task.FromResult(Metadata.Empty),
            () => Status.DefaultSuccess,
            () => Metadata.Empty,
            () => { });
}

// ═══════════════════════════════════════════════════════════════════════════
// UserGrpcService/UserGrpcServiceClient
// ═══════════════════════════════════════════════════════════════════════════

public class UserGrpcServiceClientCoverageTests
{
    private static UserGrpcService.UserGrpcServiceClient CreateClient()
    {
        var m = new Mock<CallInvoker>();
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetUserByIdRequest, UserResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetUserByIdRequest>()))
            .Returns(new UserResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetUserByIdRequest, UserResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetUserByIdRequest>()))
            .Returns(FakeCall.Of(new UserResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetUsersByIdsRequest, UsersResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetUsersByIdsRequest>()))
            .Returns(new UsersResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetUsersByIdsRequest, UsersResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetUsersByIdsRequest>()))
            .Returns(FakeCall.Of(new UsersResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<CheckUserExistsRequest, CheckUserExistsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserExistsRequest>()))
            .Returns(new CheckUserExistsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<CheckUserExistsRequest, CheckUserExistsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserExistsRequest>()))
            .Returns(FakeCall.Of(new CheckUserExistsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<RecordAuditRequest, RecordAuditResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<RecordAuditRequest>()))
            .Returns(new RecordAuditResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<RecordAuditRequest, RecordAuditResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<RecordAuditRequest>()))
            .Returns(FakeCall.Of(new RecordAuditResponse()));
        return new UserGrpcService.UserGrpcServiceClient(m.Object);
    }

    [Fact] public void GetUserById_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetUserById(new GetUserByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetUserByIdAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetUserByIdAsync(new GetUserByIdRequest()).Should().NotBeNull();

    [Fact] public void GetUserByIdAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetUserByIdAsync(new GetUserByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetUsersByIds_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetUsersByIds(new GetUsersByIdsRequest()).Should().NotBeNull();

    [Fact] public void GetUsersByIds_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetUsersByIds(new GetUsersByIdsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetUsersByIdsAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetUsersByIdsAsync(new GetUsersByIdsRequest()).Should().NotBeNull();

    [Fact] public void GetUsersByIdsAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetUsersByIdsAsync(new GetUsersByIdsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void CheckUserExists_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().CheckUserExists(new CheckUserExistsRequest()).Should().NotBeNull();

    [Fact] public void CheckUserExistsAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().CheckUserExistsAsync(new CheckUserExistsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void RecordAudit_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().RecordAudit(new RecordAuditRequest()).Should().NotBeNull();

    [Fact] public void RecordAudit_WithCallOptions_ReturnsResponse() =>
        CreateClient().RecordAudit(new RecordAuditRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void RecordAuditAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().RecordAuditAsync(new RecordAuditRequest()).Should().NotBeNull();

    [Fact] public void RecordAuditAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().RecordAuditAsync(new RecordAuditRequest(), new CallOptions()).Should().NotBeNull();
}

// ═══════════════════════════════════════════════════════════════════════════
// CollaborationRequestGrpcService/CollaborationRequestGrpcServiceClient
// ═══════════════════════════════════════════════════════════════════════════

public class CollaborationRequestGrpcServiceClientCoverageTests
{
    private static CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient CreateClient()
    {
        var m = new Mock<CallInvoker>();
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetRequestByIdRequest, CollaborationRequestResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestByIdRequest>()))
            .Returns(new CollaborationRequestResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetRequestByIdRequest, CollaborationRequestResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestByIdRequest>()))
            .Returns(FakeCall.Of(new CollaborationRequestResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetRequestsByTeamRequest, CollaborationRequestsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestsByTeamRequest>()))
            .Returns(new CollaborationRequestsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetRequestsByTeamRequest, CollaborationRequestsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestsByTeamRequest>()))
            .Returns(FakeCall.Of(new CollaborationRequestsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetRequestsByUserRequest, CollaborationRequestsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestsByUserRequest>()))
            .Returns(new CollaborationRequestsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetRequestsByUserRequest, CollaborationRequestsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetRequestsByUserRequest>()))
            .Returns(FakeCall.Of(new CollaborationRequestsResponse()));
        return new CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient(m.Object);
    }

    [Fact] public void Constructor_WithCallInvoker_CreatesClient() =>
        CreateClient().Should().NotBeNull();

    [Fact] public void GetRequestById_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetRequestById(new GetRequestByIdRequest()).Should().NotBeNull();

    [Fact] public void GetRequestById_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetRequestById(new GetRequestByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetRequestByIdAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetRequestByIdAsync(new GetRequestByIdRequest()).Should().NotBeNull();

    [Fact] public void GetRequestByIdAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetRequestByIdAsync(new GetRequestByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetRequestsByTeam_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetRequestsByTeam(new GetRequestsByTeamRequest()).Should().NotBeNull();

    [Fact] public void GetRequestsByTeam_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetRequestsByTeam(new GetRequestsByTeamRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetRequestsByTeamAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetRequestsByTeamAsync(new GetRequestsByTeamRequest()).Should().NotBeNull();

    [Fact] public void GetRequestsByTeamAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetRequestsByTeamAsync(new GetRequestsByTeamRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetRequestsByUser_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetRequestsByUser(new GetRequestsByUserRequest()).Should().NotBeNull();

    [Fact] public void GetRequestsByUser_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetRequestsByUser(new GetRequestsByUserRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetRequestsByUserAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetRequestsByUserAsync(new GetRequestsByUserRequest()).Should().NotBeNull();

    [Fact] public void GetRequestsByUserAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetRequestsByUserAsync(new GetRequestsByUserRequest(), new CallOptions()).Should().NotBeNull();
}

// ═══════════════════════════════════════════════════════════════════════════
// PostGrpcService/PostGrpcServiceClient
// ═══════════════════════════════════════════════════════════════════════════

public class PostGrpcServiceClientCoverageTests
{
    private static PostGrpcService.PostGrpcServiceClient CreateClient()
    {
        var m = new Mock<CallInvoker>();
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetPostsPagedRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsPagedRequest>()))
            .Returns(new PagedPostsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetPostsPagedRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsPagedRequest>()))
            .Returns(FakeCall.Of(new PagedPostsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetPostByIdRequest, PostResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostByIdRequest>()))
            .Returns(new PostResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetPostByIdRequest, PostResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostByIdRequest>()))
            .Returns(FakeCall.Of(new PostResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetPostsByAuthorRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsByAuthorRequest>()))
            .Returns(new PagedPostsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetPostsByAuthorRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsByAuthorRequest>()))
            .Returns(FakeCall.Of(new PagedPostsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetPostsByTeamRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsByTeamRequest>()))
            .Returns(new PagedPostsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetPostsByTeamRequest, PagedPostsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetPostsByTeamRequest>()))
            .Returns(FakeCall.Of(new PagedPostsResponse()));
        return new PostGrpcService.PostGrpcServiceClient(m.Object);
    }

    [Fact] public void Constructor_WithCallInvoker_CreatesClient() =>
        CreateClient().Should().NotBeNull();

    [Fact] public void GetPostsPaged_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetPostsPaged(new GetPostsPagedRequest()).Should().NotBeNull();

    [Fact] public void GetPostsPaged_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetPostsPaged(new GetPostsPagedRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostsPagedAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetPostsPagedAsync(new GetPostsPagedRequest()).Should().NotBeNull();

    [Fact] public void GetPostsPagedAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetPostsPagedAsync(new GetPostsPagedRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostById_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetPostById(new GetPostByIdRequest()).Should().NotBeNull();

    [Fact] public void GetPostById_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetPostById(new GetPostByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostByIdAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetPostByIdAsync(new GetPostByIdRequest()).Should().NotBeNull();

    [Fact] public void GetPostByIdAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetPostByIdAsync(new GetPostByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostsByAuthor_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetPostsByAuthor(new GetPostsByAuthorRequest()).Should().NotBeNull();

    [Fact] public void GetPostsByAuthor_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetPostsByAuthor(new GetPostsByAuthorRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostsByAuthorAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetPostsByAuthorAsync(new GetPostsByAuthorRequest()).Should().NotBeNull();

    [Fact] public void GetPostsByAuthorAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetPostsByAuthorAsync(new GetPostsByAuthorRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostsByTeam_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetPostsByTeam(new GetPostsByTeamRequest()).Should().NotBeNull();

    [Fact] public void GetPostsByTeam_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetPostsByTeam(new GetPostsByTeamRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetPostsByTeamAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetPostsByTeamAsync(new GetPostsByTeamRequest()).Should().NotBeNull();

    [Fact] public void GetPostsByTeamAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetPostsByTeamAsync(new GetPostsByTeamRequest(), new CallOptions()).Should().NotBeNull();
}
