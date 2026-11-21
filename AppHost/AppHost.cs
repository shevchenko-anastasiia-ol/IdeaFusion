using AppHost.Extensions;
using Docker.DotNet.Models;
using System.Text.RegularExpressions;

var builder = DistributedApplication.CreateBuilder(args);

// Keycloak derives the realm it imports from the file name, so this name is load-bearing.
const string RealmFileName = "IdeaFusion-realm.json";

// ============================================
// 1. DATABASE CONTAINERS CONFIGURATION
// ============================================

var postgresUser = builder.AddParameter("postgres-username", "postgres", secret: true);
var postgresPass = builder.AddParameter("postgres-password", "1234567890", secret: true);
var mongo = builder.AddMongoDB("mongodb").WithDataVolume();

var redisPass = builder.AddParameter("redis-password", "redis123", secret: true);

var rabbitUser = builder.AddParameter("rabbitmq-username", "guest", secret: true);
var rabbitPass = builder.AddParameter("rabbitmq-password", "guest", secret: true);

var keycloakAdminUser = builder.AddParameter("keycloak-admin-username", "admin", secret: true);
var keycloakAdminPass = builder.AddParameter("keycloak-admin-password", "admin", secret: true);

// Values injected into infrastructure/IdeaFusion-realm.json as ${...} placeholders.
// Override them outside git: dotnet user-secrets set "google-client-id" "<value>" --project AppHost
var smtpHost = builder.AddParameter("smtp-host", "smtp.gmail.com");
var smtpPort = builder.AddParameter("smtp-port", "587");
var smtpUser = builder.AddParameter("smtp-user", string.Empty, secret: true);
var smtpAppPassword = builder.AddParameter("smtp-app-password", string.Empty, secret: true);
var googleClientId = builder.AddParameter("google-client-id", "REPLACE_ME", secret: true);
var googleClientSecret = builder.AddParameter("google-client-secret", "REPLACE_ME", secret: true);
var keycloakAdminClientSecret = builder.AddParameter("keycloak-admin-client-secret", "REPLACE_ME", secret: true);

var postgres = builder.AddPostgres("postgres",
        userName: postgresUser,
        password: postgresPass)
    // pgvector/pgvector:pg17-trixie (the "vector" extension of copy detection) plus the hunspell uk_UA
    // dictionary of search, see infrastructure/postgres/Dockerfile. The trixie base matches the glibc
    // of the official postgres:17 image the data volume was created with; a bookworm base would
    // change text collation under existing indexes.
    .WithDockerfile("../infrastructure/postgres")
    .WithDataVolume();

// Keycloak imports a realm only when it does not exist yet. The data volume is what makes the
// realm survive restarts, so it also means edits to infrastructure/IdeaFusion-realm.json are
// ignored on the next start. Use the "Reset" action in the Aspire dashboard (or delete the
// keycloak volume) to force a re-import.
// GetValueAsync is what honours user-secrets and appsettings; the synchronous Value property
// only hands back the declared default ("REPLACE_ME"). ParameterValue also accepts the flat
// configuration key, because AddParameter looks under "Parameters:{name}" while
// `dotnet user-secrets set "google-client-id" ...` writes it unprefixed.
var smtpHostValue = ParameterValue(builder, "smtp-host", await smtpHost.Resource.GetValueAsync(default)) ?? "smtp.gmail.com";
var smtpPortValue = ParameterValue(builder, "smtp-port", await smtpPort.Resource.GetValueAsync(default)) ?? "587";
var smtpUserValue = ParameterValue(builder, "smtp-user", await smtpUser.Resource.GetValueAsync(default)) ?? string.Empty;
var smtpPasswordValue = ParameterValue(builder, "smtp-app-password", await smtpAppPassword.Resource.GetValueAsync(default)) ?? string.Empty;
if (string.IsNullOrWhiteSpace(smtpUserValue) || string.IsNullOrWhiteSpace(smtpPasswordValue))
    Console.WriteLine("[AppHost] smtp-user / smtp-app-password are not set: no emails will be sent. " +
                      "See infrastructure/README.md (dotnet user-secrets set \"smtp-user\" ... --project AppHost).");

var realmImportDirectory = ResolveRealmImport(new Dictionary<string, string?>
{
    ["SMTP_HOST"] = smtpHostValue,
    ["SMTP_PORT"] = smtpPortValue,
    ["SMTP_USER"] = smtpUserValue,
    ["SMTP_APP_PASSWORD"] = smtpPasswordValue,
    ["GOOGLE_CLIENT_ID"] = ParameterValue(builder, "google-client-id", await googleClientId.Resource.GetValueAsync(default)),
    ["GOOGLE_CLIENT_SECRET"] = ParameterValue(builder, "google-client-secret", await googleClientSecret.Resource.GetValueAsync(default)),
    ["KEYCLOAK_ADMIN_CLIENT_SECRET"] = ParameterValue(builder, "keycloak-admin-client-secret", await keycloakAdminClientSecret.Resource.GetValueAsync(default)),
});

var keycloak = builder.AddKeycloak("keycloak", port: 8080, keycloakAdminUser, keycloakAdminPass)
    .WithDataVolume()
    .WithRealmImport(realmImportDirectory)
    .WithEnvironment("SMTP_HOST", smtpHost)
    .WithEnvironment("SMTP_PORT", smtpPort)
    .WithEnvironment("SMTP_USER", smtpUser)
    .WithEnvironment("SMTP_APP_PASSWORD", smtpAppPassword)
    .WithEnvironment("GOOGLE_CLIENT_ID", googleClientId)
    .WithEnvironment("GOOGLE_CLIENT_SECRET", googleClientSecret)
    .WithEnvironment("KEYCLOAK_ADMIN_CLIENT_SECRET", keycloakAdminClientSecret);

var keycloakUrl = keycloak.GetEndpoint("http");
var keycloakRealm = "IdeaFusion";
var keycloakAudience = "ideafusion_api";

var collaborationDb = mongo.AddDatabase("collaboration-db");
var contentDb = postgres.AddDatabase("contentdb");
var identityDb = postgres.AddDatabase("identityservicedb");
var imageCopyDb = postgres.AddDatabase("imagecopydb");
var searchDb = postgres.AddDatabase("searchdb");

var redis = builder.AddRedis("redis", password: redisPass)
    .WithDataVolume()
    .WithRedisCommander();

var rabbitmq = builder.AddRabbitMQ("rabbitmq",
        userName: rabbitUser,
        password: rabbitPass)
    .WithManagementPlugin()
    .WithDataVolume();

var minio = builder.AddContainer("minio", "quay.io/minio/minio")
    .WithArgs("server", "/data")
    .WithEndpoint(9000, 9000)
    .WithVolume("minio-data", "/data");

// Image copy detection (ml/service): Python FastAPI, its own pgvector database, reads post
// images from MinIO. Model artefacts are mounted read-only from ml/models (see ml/models/README.md).
var mlService = builder.AddDockerfile("mlservice", "../ml", "service/Dockerfile")
    .WithHttpEndpoint(port: 5010, targetPort: 8000, name: "http")
    .WithBindMount("../ml/models", "/models", isReadOnly: true)
    .WithReference(imageCopyDb)
    .WithEnvironment("Minio__Endpoint", "minio:9000")
    // Aspire does not create the database itself; the service creates it on first start.
    .WaitFor(postgres)
    .WaitFor(minio)
    .WithHttpHealthCheck("/health", endpointName: "http");

var keycloakAdminClientSecretValue = ParameterValue(builder, "keycloak-admin-client-secret",
    await keycloakAdminClientSecret.Resource.GetValueAsync(default)) ?? string.Empty;

var identityService = builder.AddProject<Projects.IdentityAPI>("identityservice")
    .WithReference(identityDb)           
    .WithReference(redis) 
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(identityDb)                   
    .WaitFor(redis)
    .WaitFor(keycloak)
    .WaitFor(rabbitmq)
    .WithHttpEndpoint(port: 5002, name: "identity-http")
    .WithHttpsEndpoint(port: 7048, name: "identity-https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    // The service-account client IdentityService uses for the Keycloak admin API (2FA status,
    // user search, role changes); without it every such call fails. The resolved value, not the
    // parameter: like the realm import, it honours the flat user-secrets key.
    .WithEnvironment("Keycloak__Admin__ClientSecret", keycloakAdminClientSecretValue)
    .WithHttpHealthCheck("/health", endpointName: "identity-http");

// Collaboration  Service (uses MongoDB)
var collaborationService = builder.AddProject<Projects.Collaboration_API>("collaborationservice")
    .WithReference(collaborationDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(identityService)
    .WithReference(keycloak)
    .WaitFor(collaborationDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(identityService)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5003, name: "collaborations-http")
    .WithHttpsEndpoint(port: 7047, name: "collaborations-https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    .WithEnvironment("Grpc__IdentityService", "https://_identity-https.identityservice")
    // Email notifications (invitations, requests, direct messages) use the same Gmail account
    // as Keycloak: set smtp-user and smtp-app-password once in AppHost user secrets.
    // Resolved values, as for the realm import: the parameter resources only look under
    // "Parameters:smtp-user", while the README's `dotnet user-secrets set "smtp-user"` writes the
    // flat key, so passing the resources handed Collaboration empty strings and no mail was sent.
    .WithEnvironment("Email__Host", smtpHostValue)
    .WithEnvironment("Email__Port", smtpPortValue)
    .WithEnvironment("Email__User", smtpUserValue)
    .WithEnvironment("Email__Password", smtpPasswordValue)
    .WithEnvironment("Email__FrontendUrl", "http://localhost:5173")
    .WithHttpHealthCheck("/health", endpointName: "collaborations-http");

// Content Service (PostgreSQL)
var contentService = builder.AddProject<Projects.ContentAPI>("contentservice")
    .WithReference(contentDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(collaborationService) 
    .WithReference(identityService) 
    .WithReference(keycloak)
    .WaitFor(contentDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(collaborationService)  
    .WaitFor(identityService) 
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5005, name: "content-http")
    .WithHttpsEndpoint(port: 7050, name: "content-https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    .WithEnvironment("Grpc__IdentityService", "https://_identity-https.identityservice")
    .WithEnvironment("Grpc__CollaborationService", "https://_collaborations-https.collaborationservice")
    // Advisory only: no WaitFor, posts are published even while the ML service is down.
    .WithReference(mlService.GetEndpoint("http"))
    .WithEnvironment("CopyDetection__BaseUrl", "http://mlservice")
    // Moderation letters to authors (e.g. a post confirmed as a copy): same Gmail account.
    .WithEnvironment("Email__Host", smtpHostValue)
    .WithEnvironment("Email__Port", smtpPortValue)
    .WithEnvironment("Email__User", smtpUserValue)
    .WithEnvironment("Email__Password", smtpPasswordValue)
    .WithEnvironment("Email__FrontendUrl", "http://localhost:5173")
    .WithHttpHealthCheck("/health", endpointName: "content-http");

var aggregatorService = builder.AddProject<Projects.AggregatorService>("aggregator")
    .WithReference(contentService)
    .WithReference(collaborationService)
    .WithReference(identityService)
    .WithReference(keycloak)
    .WaitFor(contentService)
    .WaitFor(collaborationService)
    .WaitFor(identityService)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5004, name: "aggregator-http")
    .WithHttpsEndpoint(port: 7049, name: "aggregator-https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health", endpointName: "aggregator-http");

// Search (section 6.2): its own index in searchdb, rebuilt from read-only reads of the other
// services' databases. No WaitFor on the sources: a source that is down keeps its old documents.
var searchService = builder.AddProject<Projects.SearchService>("searchservice")
    .WithReference(searchDb)
    .WithReference(contentDb)
    .WithReference(identityDb)
    .WithReference(collaborationDb)
    .WithReference(keycloak)
    .WaitFor(searchDb)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5006, name: "search-http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    .WithHttpHealthCheck("/health", endpointName: "search-http");

builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(contentService)
    .WithReference(collaborationService)
    .WithReference(identityService)
    .WithReference(aggregatorService)
    .WithReference(searchService)
    .WithReference(keycloak)
    .WaitFor(aggregatorService)
    .WaitFor(contentService)
    .WaitFor(collaborationService)
    .WaitFor(identityService)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5000, name: "gateway-http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    .WithHttpHealthCheck("/health", endpointName: "gateway-http");

// A parameter is only useful here if it actually carries a supplied value, so an untouched
// default is reported as missing rather than written into the realm.
static string? ParameterValue(IDistributedApplicationBuilder builder, string name, string? parameterValue)
{
    var configured = builder.Configuration[name]
                     ?? builder.Configuration[$"Parameters:{name}"];

    if (!string.IsNullOrWhiteSpace(configured))
        return configured;

    return IsPlaceholder(parameterValue) ? null : parameterValue;
}

static bool IsPlaceholder(string? value) =>
    string.IsNullOrWhiteSpace(value) || value is "REPLACE_ME";

// Keycloak does not expand ${...} inside an imported realm file: it stores the literal text,
// so a placeholder left in the Google IdP config becomes an OAuth client Google has never
// heard of and login fails with "invalid_client / The OAuth client was not found". The
// tracked realm file keeps its placeholders (no secrets in git) and the substitution happens
// here, into a generated directory that is never referenced from source control.
static string ResolveRealmImport(IDictionary<string, string?> substitutions)
{
    var baseDirectory = AppContext.BaseDirectory;
    string? infrastructureDirectory = null;

    foreach (var candidate in new[]
             {
                 Path.Combine(baseDirectory, "..", "..", "..", "infrastructure"),
                 Path.Combine(baseDirectory, "..", "..", "..", "..", "infrastructure"),
                 Path.Combine(Directory.GetCurrentDirectory(), "infrastructure"),
             })
    {
        var full = Path.GetFullPath(candidate);
        if (File.Exists(Path.Combine(full, RealmFileName)))
        {
            infrastructureDirectory = full;
            break;
        }
    }

    if (infrastructureDirectory is null)
        throw new FileNotFoundException(
            $"Could not locate {RealmFileName} in a directory named 'infrastructure'.");

    var importDirectory = Path.Combine(Path.GetTempPath(), $"ideafusion-realm-{Guid.NewGuid():N}");
    Directory.CreateDirectory(importDirectory);

    var content = File.ReadAllText(Path.Combine(infrastructureDirectory, RealmFileName));
    foreach (var (name, value) in substitutions)
    {
        if (!string.IsNullOrEmpty(value))
            content = content.Replace($"${{{name}}}", value);
    }

    // A placeholder that survived means the value was never supplied, and the realm
    // would store that literal text. The Google broker then fails with
    // "invalid_client / The OAuth client was not found", so it is better to stop here.
    var unresolved = Regex.Matches(content, @"\$\{(?<name>[A-Z_]+)\}")
        .Select(m => m.Groups["name"].Value)
        .Distinct()
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToArray();

    if (unresolved.Length > 0)
    {
        throw new InvalidOperationException(
            "infrastructure/" + RealmFileName + " still contains unresolved placeholders: " +
            string.Join(", ", unresolved) +
            ". Supply them with: dotnet user-secrets set \"<name>\" \"<value>\" --project AppHost");
    }

    File.WriteAllText(Path.Combine(importDirectory, RealmFileName), content);

    return importDirectory;
}

await builder.Build().RunAsync();

// SQL Server container with persistent volume and secrets
/*var postgresPassword  = builder.AddParameter("postgres-password", secret: true);
var postgres  = builder.AddPostgres("postgres")
    .WithPassword(postgresPassword)
    .WithDataVolume();

// MongoDB container with persistent volume
var mongo = builder.AddMongoDB("mongodb")
    .WithDataVolume();
*/
// Create databases


// ============================================
// 2. INFRASTRUCTURE SERVICES
// ============================================

/*var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

var rabbitmq = builder.AddRabbitMQ("rabbitmq",
        userName: builder.AddParameter("rabbitmq-username", secret: true),
        password: builder.AddParameter("rabbitmq-password", secret: true))
    .WithManagementPlugin()
    .WithDataVolume();

// ============================================
// 3. MICROSERVICES REGISTRATION
// ============================================

// Identity Service (PostgreSQL)
var identityService = builder.AddProject<Projects.IdentityAPI>("identityservice")
    .WithReference(identityDb)           // ← PostgreSQL
    .WithReference(redis)                // ← для зберігання refresh-токенів / blacklist JWT
    .WaitFor(postgres)                   // ← чекаємо на БД
    .WaitFor(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health");

// Collaboration  Service (uses MongoDB)
var collaborationService = builder.AddProject<Projects.Collaboration_API>("collaborationservice")
    .WithReference(collaborationDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(identityService)
    .WaitFor(mongo)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(identityService)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health");

// Content Service (PostgreSQL)
var contentService = builder.AddProject<Projects.ContentAPI>("contentservice")
    .WithReference(contentDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(collaborationService) 
    .WithReference(identityService) 
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(collaborationService)  
    .WaitFor(identityService) 
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health");



// ============================================
// 4. AGGREGATOR SERVICE
// ============================================

var aggregatorService = builder.AddProject<Projects.AggregatorService>("aggregator")
    .WithReference(contentService)
    .WithReference(collaborationService)
    .WaitFor(contentService)
    .WaitFor(collaborationService)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health");

// ============================================
// 5. API GATEWAY
// ============================================

var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(contentService)
    .WithReference(collaborationService)
    .WithReference(aggregatorService)
    .WaitFor(aggregatorService)
    .WaitFor(contentService)
    .WaitFor(collaborationService)
    .WithHttpEndpoint(port: 5000, name: "gateway")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpHealthCheck("/health");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data")
    .WithEndpoint(9000, 9000);

builder.Build().Run();*/