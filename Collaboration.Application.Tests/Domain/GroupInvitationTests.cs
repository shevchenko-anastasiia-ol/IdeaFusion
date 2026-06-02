using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class GroupInvitationTests
{
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Developer", "Welcome!", 7);

        inv.TeamId.Should().Be("team-1");
        inv.InvitedUserId.Should().Be("user-1");
        inv.InvitedByUserId.Should().Be("admin");
        inv.Role.Should().Be("Developer");
        inv.Message.Should().Be("Welcome!");
        inv.Status.Should().Be(InvitationStatus.Pending);
        inv.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        inv.ResolvedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTeamId_ThrowsDomainException(string teamId)
    {
        var act = () => new GroupInvitation(teamId, "user-1", "admin", "Dev", null);
        act.Should().Throw<DomainException>().WithMessage("*TeamId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyInvitedUserId_ThrowsDomainException(string userId)
    {
        var act = () => new GroupInvitation("team-1", userId, "admin", "Dev", null);
        act.Should().Throw<DomainException>().WithMessage("*InvitedUserId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyRole_ThrowsDomainException(string role)
    {
        var act = () => new GroupInvitation("team-1", "user-1", "admin", role, null);
        act.Should().Throw<DomainException>().WithMessage("*Role*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidExpirationDays_ThrowsDomainException(int days)
    {
        var act = () => new GroupInvitation("team-1", "user-1", "admin", "Dev", null, days);
        act.Should().Throw<DomainException>().WithMessage("*greater than 0*");
    }

    [Fact]
    public void Accept_WhenPendingAndNotExpired_SetsStatusAccepted()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.Accept("user-1");

        inv.Status.Should().Be(InvitationStatus.Accepted);
        inv.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Decline_WhenPendingAndNotExpired_SetsStatusDeclined()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.Decline("user-1");

        inv.Status.Should().Be(InvitationStatus.Declined);
        inv.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Revoke_WhenPending_SetsStatusRevoked()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.Revoke("admin");

        inv.Status.Should().Be(InvitationStatus.Revoked);
        inv.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_WhenAlreadyDeclined_ThrowsDomainException()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.Decline("user-1");

        var act = () => inv.Accept("user-1");
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsDomainException()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.Revoke("admin");

        var act = () => inv.Revoke("admin");
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void IsExpired_WhenExpiresInFuture_ReturnsFalse()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        inv.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiredInPast_ReturnsTrue()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        SetExpiresAt(inv, DateTime.UtcNow.AddDays(-1));
        inv.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void MarkAsExpired_WhenPendingAndPastExpiry_SetsExpiredStatus()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        SetExpiresAt(inv, DateTime.UtcNow.AddDays(-1));

        inv.MarkAsExpired();

        inv.Status.Should().Be(InvitationStatus.Expired);
        inv.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsExpired_WhenNotYetExpired_DoesNotChangeStatus()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);

        inv.MarkAsExpired();

        inv.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Accept_WhenExpired_ThrowsDomainException()
    {
        var inv = new GroupInvitation("team-1", "user-1", "admin", "Dev", null, 7);
        SetExpiresAt(inv, DateTime.UtcNow.AddDays(-1));

        var act = () => inv.Accept("user-1");
        act.Should().Throw<DomainException>().WithMessage("*expired*");
    }

    private static void SetExpiresAt(GroupInvitation inv, DateTime value)
    {
        typeof(GroupInvitation)
            .GetProperty(nameof(GroupInvitation.ExpiresAt))!
            .SetValue(inv, value);
    }
}