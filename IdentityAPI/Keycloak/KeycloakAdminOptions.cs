namespace IdentityAPI.Keycloak;

/// <summary>
/// Credentials for the <c>ideafusion-admin</c> service account, used only for the
/// admin REST calls this service makes (user search, role grants, OTP status).
/// The account authenticates with the client_credentials grant, so no user
/// password ever passes through this service and the realm can keep
/// <c>directAccessGrantsEnabled</c> off.
/// </summary>
public sealed class KeycloakAdminOptions
{
    public const string SectionName = "Keycloak:Admin";

    /// <summary>Confidential client id of the service account.</summary>
    public string ClientId { get; set; } = "ideafusion-admin";

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Renew the service-account token this many seconds before it actually
    /// expires, so a request never starts with a token that dies mid-flight.
    /// </summary>
    public int TokenRefreshSkewSeconds { get; set; } = 60;
}
