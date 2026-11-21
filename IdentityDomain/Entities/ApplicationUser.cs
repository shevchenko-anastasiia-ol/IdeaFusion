using Microsoft.AspNetCore.Identity;

namespace IdentityServiceDomain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AvatarUrl { get; set; }
    public string? FullName { get; set; }
    public string? Specialization { get; set; }

    /// <summary>
    /// Passwords live in Keycloak. This column stays as a sentinel ("!") so the
    /// ASP.NET Identity user store never treats a row as password-less or
    /// unvalidated, which keeps <c>UserManager</c> queries working.
    /// </summary>
    public const string ExternalPasswordSentinel = "!";
}