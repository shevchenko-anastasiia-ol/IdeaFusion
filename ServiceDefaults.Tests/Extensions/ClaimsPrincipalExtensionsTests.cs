using FluentAssertions;
using ServiceDefaults.Extensions;
using System.Security.Claims;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    // ── GetUserId ────────────────────────────────────────────────────────────

    [Fact]
    public void GetUserId_WithSubClaim_ReturnsGuid()
    {
        var id   = Guid.NewGuid();
        var user = MakeUser(("sub", id.ToString()));

        user.GetUserId().Should().Be(id);
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsGuid()
    {
        var id   = Guid.NewGuid();
        var user = MakeUser((ClaimTypes.NameIdentifier, id.ToString()));

        user.GetUserId().Should().Be(id);
    }

    [Fact]
    public void GetUserId_SubClaimTakesPrecedenceOverNameIdentifier()
    {
        var subId  = Guid.NewGuid();
        var nameId = Guid.NewGuid();
        var user   = MakeUser(("sub", subId.ToString()), (ClaimTypes.NameIdentifier, nameId.ToString()));

        user.GetUserId().Should().Be(subId);
    }

    [Fact]
    public void GetUserId_NoClaim_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser();
        var act  = () => user.GetUserId();

        act.Should().Throw<UnauthorizedAccessException>()
           .WithMessage("*claim not found*");
    }

    [Fact]
    public void GetUserId_EmptySubClaim_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser(("sub", ""));
        var act  = () => user.GetUserId();

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void GetUserId_NonGuidValue_ThrowsInvalidOperationException()
    {
        var user = MakeUser(("sub", "not-a-guid"));
        var act  = () => user.GetUserId();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*not a valid GUID*");
    }

    // ── TryGetUserId ─────────────────────────────────────────────────────────

    [Fact]
    public void TryGetUserId_WithSubClaim_ReturnsGuid()
    {
        var id   = Guid.NewGuid();
        var user = MakeUser(("sub", id.ToString()));

        user.TryGetUserId().Should().Be(id);
    }

    [Fact]
    public void TryGetUserId_WithNameIdentifierClaim_ReturnsGuid()
    {
        var id   = Guid.NewGuid();
        var user = MakeUser((ClaimTypes.NameIdentifier, id.ToString()));

        user.TryGetUserId().Should().Be(id);
    }

    [Fact]
    public void TryGetUserId_NoClaim_ReturnsNull()
    {
        var user = MakeUser();
        user.TryGetUserId().Should().BeNull();
    }

    [Fact]
    public void TryGetUserId_InvalidGuid_ReturnsNull()
    {
        var user = MakeUser(("sub", "bad-value"));
        user.TryGetUserId().Should().BeNull();
    }

    // ── GetUserEmail ──────────────────────────────────────────────────────────

    [Fact]
    public void GetUserEmail_WithEmailClaim_ReturnsEmail()
    {
        var user = MakeUser(("email", "alice@example.com"));
        user.GetUserEmail().Should().Be("alice@example.com");
    }

    [Fact]
    public void GetUserEmail_WithClaimTypesEmail_ReturnsEmail()
    {
        var user = MakeUser((ClaimTypes.Email, "bob@example.com"));
        user.GetUserEmail().Should().Be("bob@example.com");
    }

    [Fact]
    public void GetUserEmail_NoClaim_ReturnsNull()
    {
        var user = MakeUser();
        user.GetUserEmail().Should().BeNull();
    }

    // ── GetUserName ───────────────────────────────────────────────────────────

    [Fact]
    public void GetUserName_WithPreferredUsernameClaim_ReturnsName()
    {
        var user = MakeUser(("preferred_username", "alice"));
        user.GetUserName().Should().Be("alice");
    }

    [Fact]
    public void GetUserName_NoClaims_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        user.GetUserName().Should().BeNull();
    }

    // ── IsInRole ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsInRole_WithClaimTypesRole_ReturnsTrue()
    {
        var user = MakeUser((ClaimTypes.Role, "admin"));
        user.IsInRole("admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WithRoleClaim_ReturnsTrue()
    {
        var user = MakeUser(("role", "admin"));
        // Must call via static method — instance ClaimsPrincipal.IsInRole takes precedence over extension
        ClaimsPrincipalExtensions.IsInRole(user, "admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_DifferentRole_ReturnsFalse()
    {
        var user = MakeUser((ClaimTypes.Role, "user"));
        user.IsInRole("admin").Should().BeFalse();
    }

    [Fact]
    public void IsInRole_NoRoleClaims_ReturnsFalse()
    {
        var user = MakeUser();
        user.IsInRole("admin").Should().BeFalse();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ClaimsPrincipal MakeUser(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)));
        return new ClaimsPrincipal(identity);
    }
}
