using System.Net;

namespace IdentityAPI.Keycloak;

/// <summary>A Keycloak user as far as this service is concerned.</summary>
public sealed class KeycloakUserSummary
{
    public Guid Id { get; init; }

    public string? Username { get; init; }

    public string? Email { get; init; }

    public bool EmailVerified { get; init; }

    public bool Enabled { get; init; }

    public IReadOnlyList<string> RealmRoles { get; init; } = [];

    /// <summary>
    /// Whether an authenticator app is set up: the user has an "otp" credential
    /// in Keycloak.
    /// </summary>
    public bool TwoFactorConfigured { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Why a Keycloak admin call could not be completed.</summary>
public class KeycloakAdminException : Exception
{
    public KeycloakAdminException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public interface IKeycloakAdminClient
{
    /// <summary>Where a user goes to manage their own account, including setting up 2FA.</summary>
    string AccountConsoleUrl { get; }

    Task<IReadOnlyList<KeycloakUserSummary>> SearchUsersAsync(
        string? search,
        int first = 0,
        int max = 25,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Grants realm roles. Returns the effective realm roles afterwards.</summary>
    Task<IReadOnlyList<string>> AddRolesAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes realm roles. Returns the effective realm roles afterwards.</summary>
    Task<IReadOnlyList<string>> RemoveRolesAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<bool> IsOtpConfiguredAsync(Guid userId, CancellationToken cancellationToken = default);
}
