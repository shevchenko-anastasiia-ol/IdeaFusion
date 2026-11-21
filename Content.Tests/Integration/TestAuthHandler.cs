using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Content.Tests.Integration;

/// <summary>
/// Stands in for Keycloak JWT validation in integration tests. The real
/// <c>ideafusion-frontend</c> token is not available in-process, and the
/// fail-closed fallback policy rejects every anonymous request, so tests opt in
/// to a principal through headers:
///
///   X-Test-Subject  the "sub" claim (defaults to the seeded AnnaGray id)
///   X-Test-Roles    comma-separated realm roles (defaults to "author")
///   X-Test-Anonymous present =&gt; no result, i.e. a genuinely anonymous caller
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Test";

    public const string SubjectHeader = "X-Test-Subject";
    public const string RolesHeader   = "X-Test-Roles";
    public const string AnonymousHeader = "X-Test-Anonymous";

    public const string DefaultSubject = "22222222-2222-2222-2222-222222222222";
    public const string DefaultRole    = "author";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                           ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(AnonymousHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var subject = Request.Headers.TryGetValue(SubjectHeader, out var s)
            ? s.ToString()
            : DefaultSubject;

        var roles = Request.Headers.TryGetValue(RolesHeader, out var r)
            ? r.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : new[] { DefaultRole };

        var claims = new List<Claim>
        {
            new("sub", subject),
            new("preferred_username", "testuser"),
            new("email", "test@ideafusion.com")
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var identity = new ClaimsIdentity(claims, Scheme, "preferred_username", "role");

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}
