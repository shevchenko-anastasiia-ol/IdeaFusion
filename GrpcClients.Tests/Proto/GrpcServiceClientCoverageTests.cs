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
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<ValidateTokenRequest, ValidateTokenResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<ValidateTokenRequest>()))
            .Returns(new ValidateTokenResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<ValidateTokenRequest, ValidateTokenResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<ValidateTokenRequest>()))
            .Returns(FakeCall.Of(new ValidateTokenResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<CheckUserExistsRequest, CheckUserExistsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserExistsRequest>()))
            .Returns(new CheckUserExistsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<CheckUserExistsRequest, CheckUserExistsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserExistsRequest>()))
            .Returns(FakeCall.Of(new CheckUserExistsResponse()));
        return new UserGrpcService.UserGrpcServiceClient(m.Object);
    }

    [Fact] public void Constructor_WithCallInvoker_CreatesClient() =>
        CreateClient().Should().NotBeNull();

    [Fact] public void GetUserById_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetUserById(new GetUserByIdRequest { UserId = "u-1" }).Should().NotBeNull();

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

    [Fact] public void ValidateToken_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().ValidateToken(new ValidateTokenRequest()).Should().NotBeNull();

    [Fact] public void ValidateToken_WithCallOptions_ReturnsResponse() =>
        CreateClient().ValidateToken(new ValidateTokenRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void ValidateTokenAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().ValidateTokenAsync(new ValidateTokenRequest()).Should().NotBeNull();

    [Fact] public void ValidateTokenAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().ValidateTokenAsync(new ValidateTokenRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void CheckUserExists_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().CheckUserExists(new CheckUserExistsRequest()).Should().NotBeNull();

    [Fact] public void CheckUserExists_WithCallOptions_ReturnsResponse() =>
        CreateClient().CheckUserExists(new CheckUserExistsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void CheckUserExistsAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().CheckUserExistsAsync(new CheckUserExistsRequest()).Should().NotBeNull();

    [Fact] public void CheckUserExistsAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().CheckUserExistsAsync(new CheckUserExistsRequest(), new CallOptions()).Should().NotBeNull();

}

// ═══════════════════════════════════════════════════════════════════════════
// TeamGrpcService/TeamGrpcServiceClient
// ═══════════════════════════════════════════════════════════════════════════

public class TeamGrpcServiceClientCoverageTests
{
    private static TeamGrpcService.TeamGrpcServiceClient CreateClient()
    {
        var m = new Mock<CallInvoker>();
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetTeamsPagedRequest, PagedTeamsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamsPagedRequest>()))
            .Returns(new PagedTeamsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetTeamsPagedRequest, PagedTeamsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamsPagedRequest>()))
            .Returns(FakeCall.Of(new PagedTeamsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetTeamByIdRequest, TeamResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamByIdRequest>()))
            .Returns(new TeamResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetTeamByIdRequest, TeamResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamByIdRequest>()))
            .Returns(FakeCall.Of(new TeamResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<GetTeamsByIdsRequest, TeamsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamsByIdsRequest>()))
            .Returns(new TeamsResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<GetTeamsByIdsRequest, TeamsResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<GetTeamsByIdsRequest>()))
            .Returns(FakeCall.Of(new TeamsResponse()));
        m.Setup(x => x.BlockingUnaryCall(It.IsAny<Method<CheckUserIsMemberRequest, CheckUserIsMemberResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserIsMemberRequest>()))
            .Returns(new CheckUserIsMemberResponse());
        m.Setup(x => x.AsyncUnaryCall(It.IsAny<Method<CheckUserIsMemberRequest, CheckUserIsMemberResponse>>(),
            It.IsAny<string?>(), It.IsAny<CallOptions>(), It.IsAny<CheckUserIsMemberRequest>()))
            .Returns(FakeCall.Of(new CheckUserIsMemberResponse()));
        return new TeamGrpcService.TeamGrpcServiceClient(m.Object);
    }

    [Fact] public void Constructor_WithCallInvoker_CreatesClient() =>
        CreateClient().Should().NotBeNull();

    [Fact] public void GetTeamsPaged_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetTeamsPaged(new GetTeamsPagedRequest()).Should().NotBeNull();

    [Fact] public void GetTeamsPaged_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetTeamsPaged(new GetTeamsPagedRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetTeamsPagedAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetTeamsPagedAsync(new GetTeamsPagedRequest()).Should().NotBeNull();

    [Fact] public void GetTeamsPagedAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetTeamsPagedAsync(new GetTeamsPagedRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetTeamById_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetTeamById(new GetTeamByIdRequest()).Should().NotBeNull();

    [Fact] public void GetTeamById_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetTeamById(new GetTeamByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetTeamByIdAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetTeamByIdAsync(new GetTeamByIdRequest()).Should().NotBeNull();

    [Fact] public void GetTeamByIdAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetTeamByIdAsync(new GetTeamByIdRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetTeamsByIds_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().GetTeamsByIds(new GetTeamsByIdsRequest()).Should().NotBeNull();

    [Fact] public void GetTeamsByIds_WithCallOptions_ReturnsResponse() =>
        CreateClient().GetTeamsByIds(new GetTeamsByIdsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void GetTeamsByIdsAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().GetTeamsByIdsAsync(new GetTeamsByIdsRequest()).Should().NotBeNull();

    [Fact] public void GetTeamsByIdsAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().GetTeamsByIdsAsync(new GetTeamsByIdsRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void CheckUserIsMember_WithDefaultOptions_ReturnsResponse() =>
        CreateClient().CheckUserIsMember(new CheckUserIsMemberRequest()).Should().NotBeNull();

    [Fact] public void CheckUserIsMember_WithCallOptions_ReturnsResponse() =>
        CreateClient().CheckUserIsMember(new CheckUserIsMemberRequest(), new CallOptions()).Should().NotBeNull();

    [Fact] public void CheckUserIsMemberAsync_WithDefaultOptions_ReturnsCall() =>
        CreateClient().CheckUserIsMemberAsync(new CheckUserIsMemberRequest()).Should().NotBeNull();

    [Fact] public void CheckUserIsMemberAsync_WithCallOptions_ReturnsCall() =>
        CreateClient().CheckUserIsMemberAsync(new CheckUserIsMemberRequest(), new CallOptions()).Should().NotBeNull();
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
