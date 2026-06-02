using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Queries.CollaborationRequest;

public class CollaborationRequestQueryHandlersTests
{
    private readonly Mock<ICollaborationRequestRepository> _repoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.CollaborationRequest CreateRequest(
        string teamId = "team-1",
        string fromUserId = "user-1",
        string role = "Developer")
    {
        return new Domain.Entities.CollaborationRequest(teamId, fromUserId, role, null);
    }

    // ── GetCollaborationRequestByIdQueryHandler ───────────────────────────────

    [Fact]
    public async Task GetById_WhenRequestExists_ReturnsRequest()
    {
        var request = CreateRequest();
        _repoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(request);

        var handler = new GetCollaborationRequestByIdQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetCollaborationRequestByIdQuery { RequestId = "req-1" }, _ct);

        result.Should().NotBeNull();
        result!.TeamId.Should().Be("team-1");
    }

    [Fact]
    public async Task GetById_WhenRequestNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("missing", _ct))
                 .ReturnsAsync((Domain.Entities.CollaborationRequest?)null);

        var handler = new GetCollaborationRequestByIdQueryHandler(_repoMock.Object);

        await handler.Invoking(h => h.Handle(
                         new GetCollaborationRequestByIdQuery { RequestId = "missing" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    // ── GetCollaborationRequestsByTeamQueryHandler ────────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsRequestsFromRepository()
    {
        var requests = new[] { CreateRequest(teamId: "team-1"), CreateRequest(teamId: "team-1") };
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct))
                 .ReturnsAsync(requests);

        var handler = new GetCollaborationRequestsByTeamQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetCollaborationRequestsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.TeamId == "team-1");
    }

    [Fact]
    public async Task GetByTeam_WhenNoRequests_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct))
                 .ReturnsAsync([]);

        var handler = new GetCollaborationRequestsByTeamQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetCollaborationRequestsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().BeEmpty();
    }

    // ── GetCollaborationRequestsByUserQueryHandler ────────────────────────────

    [Fact]
    public async Task GetByUser_ReturnsRequestsFromRepository()
    {
        var requests = new[] { CreateRequest(fromUserId: "user-1"), CreateRequest(fromUserId: "user-1") };
        _repoMock.Setup(r => r.GetByFromUserIdAsync("user-1", _ct))
                 .ReturnsAsync(requests);

        var handler = new GetCollaborationRequestsByUserQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetCollaborationRequestsByUserQuery { UserId = "user-1" }, _ct);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUser_WhenNoRequests_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByFromUserIdAsync("user-1", _ct))
                 .ReturnsAsync([]);

        var handler = new GetCollaborationRequestsByUserQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetCollaborationRequestsByUserQuery { UserId = "user-1" }, _ct);

        result.Should().BeEmpty();
    }

    // ── GetCollaborationRequestsPagedQueryHandler ─────────────────────────────

    [Fact]
    public async Task GetPaged_ReturnsPagedListFromRepository()
    {
        var request = CreateRequest();
        var paged = new PagedList<Domain.Entities.CollaborationRequest>(
            [request], 1, 1, 10);
        var parameters = new CollaborationRequestParameters
        {
            TeamId = "team-1", PageNumber = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetPagedFilteredAsync(
                     "team-1", null, null, null, null,
                     1, 10, false, _ct))
                 .ReturnsAsync(paged);

        var handler = new GetCollaborationRequestsPagedQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetCollaborationRequestsPagedQuery { Parameters = parameters }, _ct);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }
}