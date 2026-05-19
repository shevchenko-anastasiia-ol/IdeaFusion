using AppHost.Extensions;
using Docker.DotNet.Models;

var builder = DistributedApplication.CreateBuilder(args);

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

var postgres = builder.AddPostgres("postgres",
        userName: postgresUser,
        password: postgresPass)
    .WithDataVolume();

var keycloak = builder.AddKeycloak("keycloak", port: 8080, keycloakAdminUser, keycloakAdminPass)
    .WithDataVolume()
    .WithAutoConfiguration();

var keycloakUrl = keycloak.GetEndpoint("http");
var keycloakRealm = "IdeaFusion";
var keycloakAudience = "ideafusion_api";

var collaborationDb = mongo.AddDatabase("collaboration-db");
var contentDb = postgres.AddDatabase("contentdb");
var identityDb = postgres.AddDatabase("identityservicedb");

var redis = builder.AddRedis("redis", password: redisPass)
    .WithDataVolume()
    .WithRedisCommander();

var rabbitmq = builder.AddRabbitMQ("rabbitmq",
        userName: rabbitUser,
        password: rabbitPass)
    .WithManagementPlugin()
    .WithDataVolume();

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data")
    .WithEndpoint(9000, 9000)
    .WithVolume("minio-data", "/data");

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
    .WithHttpHealthCheck("/health");

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
    .WithEnvironment("Grpc__IdentityService", "https://identityservice")
    .WithHttpHealthCheck("/health");

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
    .WithEnvironment("Grpc__IdentityService", "https://identityservice")
    .WithEnvironment("Grpc__CollaborationService", "https://collaborationservice")
    .WithHttpHealthCheck("/health");

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
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(contentService)
    .WithReference(collaborationService)
    .WithReference(identityService)
    .WithReference(aggregatorService)
    .WithReference(keycloak)
    .WaitFor(aggregatorService)
    .WaitFor(contentService)
    .WaitFor(collaborationService)
    .WaitFor(identityService)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5000, name: "gateway-http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithKeycloakEnvironment(keycloakUrl, keycloakRealm, keycloakAudience)
    .WithHttpHealthCheck("/health");

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