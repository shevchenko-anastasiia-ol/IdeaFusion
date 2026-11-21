using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ServiceDefaults.Auth;
using System.Security.Claims;
using Xunit;

namespace ServiceDefaults.Tests.Auth;

public class CurrentUserAccessorTests
{
    private static readonly Guid Subject = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CurrentUserAccessor Create(params Claim[] claims)
    {
        var http = new DefaultHttpContext();
        if (claims.Length > 0)
        {
            http.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "Bearer"));
        }
        return new CurrentUserAccessor(new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public void NoHttpContext_IsNotAuthenticatedAndExposesNothing()
    {
        var accessor = new CurrentUserAccessor(new HttpContextAccessor());

        accessor.IsAuthenticated.Should().BeFalse();
        accessor.UserId.Should().BeNull();
        accessor.Email.Should().BeNull();
        accessor.UserName.Should().BeNull();
        accessor.Roles.Should().BeEmpty();
        accessor.IsAuthor.Should().BeFalse();
        accessor.IsModerator.Should().BeFalse();
        accessor.IsAdministrator.Should().BeFalse();
    }

    [Fact]
    public void AnonymousUser_IsNotAuthenticated()
    {
        Create().IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void SubjectClaim_ExposesUserIdAsGuid()
    {
        Create(new Claim("sub", Subject.ToString())).UserId.Should().Be(Subject);
    }

    [Fact]
    public void MappedNameIdentifierClaim_ExposesUserIdAsGuid()
    {
        // With inbound claim mapping the "sub" arrives as ClaimTypes.NameIdentifier.
        Create(new Claim(ClaimTypes.NameIdentifier, Subject.ToString()))
            .UserId.Should().Be(Subject);
    }

    [Fact]
    public void NonGuidSubject_ExposesNullUserId()
    {
        Create(new Claim("sub", "not-a-guid")).UserId.Should().BeNull();
    }

    [Fact]
    public void PrefersSubOverNameIdentifier()
    {
        var other = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Create(new Claim("sub", Subject.ToString()),
               new Claim(ClaimTypes.NameIdentifier, other.ToString()))
            .UserId.Should().Be(Subject);
    }

    [Fact]
    public void EmailAndUserName_AreReadFromKeycloakClaims()
    {
        var accessor = Create(
            new Claim("email", "anna@ideafusion.com"),
            new Claim("preferred_username", "annogray"));

        accessor.Email.Should().Be("anna@ideafusion.com");
        accessor.UserName.Should().Be("annogray");
    }

    [Fact]
    public void Roles_AreReadFromTheFlatRoleClaim()
    {
        Create(new Claim("role", "author"),
               new Claim("role", "moderator"))
            .Roles.Should().BeEquivalentTo("author", "moderator");
    }

    [Fact]
    public void Roles_AreAlsoReadFromMappedRoleClaim()
    {
        Create(new Claim(ClaimTypes.Role, "administrator"))
            .Roles.Should().BeEquivalentTo("administrator");
    }

    [Fact]
    public void Roles_AreDeduplicatedCaseInsensitively()
    {
        Create(new Claim("role", "author"), new Claim(ClaimTypes.Role, "AUTHOR"))
            .Roles.Should().BeEquivalentTo(new[] { "author" });
    }

    [Theory]
    [InlineData("author", true, false, false)]
    [InlineData("moderator", true, true, false)]
    [InlineData("administrator", true, true, true)]
    public void RoleFlags_FollowTheCompositeHierarchy(string role, bool author, bool moderator, bool administrator)
    {
        var accessor = Create(new Claim("role", role));

        accessor.IsAuthor.Should().Be(author);
        accessor.IsModerator.Should().Be(moderator);
        accessor.IsAdministrator.Should().Be(administrator);
    }

    [Fact]
    public void MultipleRoleClaims_AreAllHonoured()
    {
        var accessor = Create(new Claim("role", "author"), new Claim("role", "administrator"));

        accessor.IsAuthor.Should().BeTrue();
        accessor.IsAdministrator.Should().BeTrue();
    }

    [Fact]
    public void IsInRole_IsCaseInsensitive()
    {
        var accessor = Create(new Claim("role", "administrator"));

        accessor.IsInRole("ADMINISTRATOR").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WithBlankRole_ReturnsFalse()
    {
        var accessor = Create(new Claim("role", "author"));

        accessor.IsInRole("").Should().BeFalse();
        accessor.IsInRole("   ").Should().BeFalse();
    }
}
