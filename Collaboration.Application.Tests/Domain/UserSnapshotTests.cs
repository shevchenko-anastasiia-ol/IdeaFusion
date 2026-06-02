using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class UserSnapshotTests
{
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        var snapshot = new UserSnapshot("user-1", "alice", "https://example.com/avatar.png");

        snapshot.UserId.Should().Be("user-1");
        snapshot.Username.Should().Be("alice");
        snapshot.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public void Constructor_WithoutAvatar_SetsAvatarUrlToNull()
    {
        var snapshot = new UserSnapshot("user-1", "alice");
        snapshot.AvatarUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyUserId_ThrowsDomainException(string userId)
    {
        var act = () => new UserSnapshot(userId, "alice");
        act.Should().Throw<DomainException>().WithMessage("*UserId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyUsername_ThrowsDomainException(string username)
    {
        var act = () => new UserSnapshot("user-1", username);
        act.Should().Throw<DomainException>().WithMessage("*Username*");
    }

    [Fact]
    public void Constructor_WithUsernameTooLong_ThrowsDomainException()
    {
        var longName = new string('a', 101);
        var act = () => new UserSnapshot("user-1", longName);
        act.Should().Throw<DomainException>().WithMessage("*exceed*");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-1", "alice");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentUserId_AreNotEqual()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-2", "alice");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentUsername_AreNotEqual()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-1", "bob");

        a.Should().NotBe(b);
    }

    [Fact]
    public void WithUpdatedUsername_ReturnsNewSnapshotWithNewName()
    {
        var original = new UserSnapshot("user-1", "alice", "https://avatar.png");
        var updated = original.WithUpdatedUsername("bob");

        updated.Username.Should().Be("bob");
        updated.UserId.Should().Be("user-1");
        updated.AvatarUrl.Should().Be("https://avatar.png");
        updated.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithUpdatedAvatar_ReturnsNewSnapshotWithNewAvatar()
    {
        var original = new UserSnapshot("user-1", "alice");
        var updated = original.WithUpdatedAvatar("https://new-avatar.png");

        updated.AvatarUrl.Should().Be("https://new-avatar.png");
        updated.UserId.Should().Be("user-1");
        updated.Username.Should().Be("alice");
        updated.Should().NotBeSameAs(original);
    }

    [Fact]
    public void ToString_ReturnsUsername()
    {
        var snapshot = new UserSnapshot("user-1", "alice");
        snapshot.ToString().Should().Be("alice");
    }

    [Fact]
    public void HashCode_SameValues_Equal()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-1", "alice");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}