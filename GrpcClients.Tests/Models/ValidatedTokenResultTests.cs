using FluentAssertions;
using GrpcClients.Models;
using Xunit;

namespace GrpcClients.Tests.Models;

public class ValidatedTokenResultTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var result = new ValidatedTokenResult(true, "user-1", "alice", "alice@example.com");

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be("user-1");
        result.UserName.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void Constructor_WithInvalidToken_SetsIsValidFalse()
    {
        var result = new ValidatedTokenResult(false, "", "", "");

        result.IsValid.Should().BeFalse();
        result.UserId.Should().BeEmpty();
        result.UserName.Should().BeEmpty();
        result.Email.Should().BeEmpty();
    }

    // ── Record equality ───────────────────────────────────────────────────────

    [Fact]
    public void TwoResultsWithSameValues_AreEqual()
    {
        var a = new ValidatedTokenResult(true, "u1", "name", "email@x.com");
        var b = new ValidatedTokenResult(true, "u1", "name", "email@x.com");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoResultsWithDifferentValues_AreNotEqual()
    {
        var a = new ValidatedTokenResult(true,  "u1", "alice", "a@x.com");
        var b = new ValidatedTokenResult(false, "u2", "bob",   "b@x.com");

        a.Should().NotBe(b);
    }

    // ── Record with expression ────────────────────────────────────────────────

    [Fact]
    public void WithExpression_CreatesNewInstanceWithUpdatedProperty()
    {
        var original = new ValidatedTokenResult(true, "u1", "alice", "alice@x.com");
        var updated  = original with { IsValid = false };

        updated.IsValid.Should().BeFalse();
        updated.UserId.Should().Be("u1");
        updated.UserName.Should().Be("alice");
        updated.Email.Should().Be("alice@x.com");

        original.IsValid.Should().BeTrue();
    }

    // ── Deconstruct ───────────────────────────────────────────────────────────

    [Fact]
    public void Deconstruct_ReturnsAllValues()
    {
        var result = new ValidatedTokenResult(true, "uid", "uname", "u@email.com");
        var (isValid, userId, userName, email) = result;

        isValid.Should().BeTrue();
        userId.Should().Be("uid");
        userName.Should().Be("uname");
        email.Should().Be("u@email.com");
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsPropertyValues()
    {
        var result = new ValidatedTokenResult(true, "u1", "alice", "alice@x.com");
        var str = result.ToString();

        str.Should().Contain("True").And.Contain("u1").And.Contain("alice");
    }
}