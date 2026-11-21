using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace ServiceDefaults.Extensions;

public static class SwaggerExtensions
    {
        /// <summary>
        /// Adds Swagger with Keycloak OAuth2 authentication support.
        /// </summary>
        public static IServiceCollection AddSwaggerWithKeycloak(
            this IServiceCollection services,
            IConfiguration configuration,
            string title,
            string version = "v1")
        {
            var keycloakUrl =
                configuration["Keycloak:Url"]
                ?? configuration["services:keycloak:https:0"]
                ?? configuration["services:keycloak:http:0"]
                ?? Environment.GetEnvironmentVariable("KEYCLOAK_URL")
                ?? "http://localhost:8080";

            var realm =
                configuration["Keycloak:Realm"]
                ?? Environment.GetEnvironmentVariable("KEYCLOAK_REALM")
                ?? "IdeaFusion";

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(version, new OpenApiInfo
                {
                    Title = title,
                    Version = version,
                    Description = $"IdeaFusion API authenticated via Keycloak (Realm: {realm})",
                });

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "OAuth2 Authorization Code Flow with PKCE via Keycloak",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{keycloakUrl}/realms/{realm}/protocol/openid-connect/auth"),
                            TokenUrl = new Uri($"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token"),

                            Scopes = new Dictionary<string, string>
                            {
                                // OpenID Connect standard scopes
                                { "openid", "OpenID Connect scope" },
                                { "profile", "User profile information" },
                                { "email", "User email address" },

                                // IdeaFusion API access
                                { "ideafusion_api", "Full access to IdeaFusion APIs" },
                            }
                        }
                    }
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        new[]
                        {
                            "openid",
                            "profile",
                            "email",
                            "ideafusion_api"
                        }
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Configures Swagger UI with Keycloak OAuth2 settings.
        /// Only enables Swagger in Development environment.
        /// </summary>
        public static WebApplication UseSwaggerWithKeycloak(
            this WebApplication app,
            IConfiguration? configuration = null)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json",
                        $"{app.Environment.ApplicationName} v1");

                    options.OAuthClientId("ideafusion-frontend");
                    options.OAuthAppName("IdeaFusion Swagger UI");
                    options.OAuthUsePkce();

                    options.OAuthScopes(
                        "openid",
                        "profile",
                        "email",
                        "ideafusion_api"
                    );

                    options.OAuthScopeSeparator(" ");

                    // Optional: Persist authorization data
                    options.EnablePersistAuthorization();

                    options.DisplayRequestDuration();
                });
            }

            return app;
        }
    }