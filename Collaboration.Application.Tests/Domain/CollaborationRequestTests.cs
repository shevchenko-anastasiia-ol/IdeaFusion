using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class CollaborationRequestTests
{
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Developer", "Hello!", "user-2", "alice", "https://avatar.png");

        req.TeamId.Should().Be("team-1");
        req.FromUserId.Should().Be("user-1");
        req.Role.Should().Be("Developer");
        req.Message.Should().Be("Hello!");
        req.ToUserId.Should().Be("user-2");
        req.FromUsername.Should().Be("alice");
        req.Status.Should().Be(CollaborationRequestStatus.Pending);
        req.ResolvedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTeamId_ThrowsDomainException(string teamId)
    {
        var act = () => new CollaborationRequest(teamId, "user-1", "Developer", null);
        act.Should().Throw<DomainException>().WithMessage("*TeamId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyRole_ThrowsDomainException(string role)
    {
        var act = () => new CollaborationRequest("team-1", "user-1", role, null);
        act.Should().Throw<DomainException>().WithMessage("*Role*");
    }

    [Fact]
    public void Constructor_WhitespaceName_IsStoredAsNull()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null, null, "   ");
        req.FromUsername.Should().BeNull();
    }

    [Fact]
    public void Accept_WhenPending_SetsStatusAccepted()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Accept("user-2");

        req.Status.Should().Be(CollaborationRequestStatus.Accepted);
        req.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WhenPending_SetsStatusRejected()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Reject("user-2");

        req.Status.Should().Be(CollaborationRequestStatus.Rejected);
        req.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenPending_SetsStatusCancelled()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Cancel("user-1");

        req.Status.Should().Be(CollaborationRequestStatus.Cancelled);
        req.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ThrowsDomainException()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Accept("user-2");

        var act = () => req.Accept("user-2");
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Reject_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Cancel("user-1");

        var act = () => req.Reject("user-2");
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Cancel_WhenAlreadyRejected_ThrowsDomainException()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Reject("user-2");

        var act = () => req.Cancel("user-1");
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Accept_SetsUpdatedBy()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        req.Accept("reviewer");

        req.UpdatedBy.Should().Be("reviewer");
    }
}