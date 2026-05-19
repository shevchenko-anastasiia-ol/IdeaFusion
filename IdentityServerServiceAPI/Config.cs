using Duende.IdentityServer.Models;

namespace IdeaFusion.IdentityServerServiceAPI;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new[]
        {
            new ApiScope("ideafusion_api", "Full access to all IdeaFusion services")
            {
                UserClaims = { "role", "name", "email" }
            }
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new[]
        {
            new ApiResource("ideafusion_api", "IdeaFusion API")
            {
                Scopes = { "ideafusion_api" },
                UserClaims = { "role", "name", "email" }
            }
        };

    public static IEnumerable<Client> Clients =>
        new[]
        {
            new Client
            {
                ClientId = "swagger",
                ClientName = "Swagger UI (All IdeaFusion Services)",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                AllowAccessTokensViaBrowser = true,
                AllowOfflineAccess = true,
                AccessTokenLifetime = 3600,

                RedirectUris =
                {
                    // Identity Service
                    "https://localhost:7052/swagger/oauth2-redirect.html",

                    // Content Service
                    "https://localhost:7101/swagger/oauth2-redirect.html",
                    "https://localhost:7102/swagger/oauth2-redirect.html",

                    // Collaboration Service
                    "https://localhost:7201/swagger/oauth2-redirect.html",
                    "https://localhost:7202/swagger/oauth2-redirect.html",

                    // Gateway
                    "https://localhost:5000/swagger/oauth2-redirect.html"
                },

                AllowedCorsOrigins =
                {
                    "https://localhost:7052",
                    "https://localhost:7101",
                    "https://localhost:7102",
                    "https://localhost:7201",
                    "https://localhost:7202",
                    "https://localhost:5000"
                },

                AllowedScopes = { "openid", "profile", "email", "ideafusion_api" }
            },

            new Client
            {
                ClientId = "postman",
                ClientName = "Postman Testing Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("postman-secret".Sha256()) },
                AllowAccessTokensViaBrowser = true,
                AccessTokenLifetime = 3600,
                AllowedScopes = { "openid", "profile", "email", "ideafusion_api" }
            }
        };
}