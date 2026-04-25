using Docker.DotNet.Models;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// 1. DATABASE CONTAINERS CONFIGURATION
// ============================================

// SQL Server container with persistent volume and secrets
var postgresPassword  = builder.AddParameter("postgres-password", secret: true);
var postgres  = builder.AddPostgres("postgres")
    .WithPassword(postgresPassword)
    .WithDataVolume();

// MongoDB container with persistent volume
var mongo = builder.AddMongoDB("mongodb")
    .WithDataVolume();

// Create databases
var collaborationDb = mongo.AddDatabase("collaboration-db");
var contentDb = postgres.AddDatabase("contentdb");

// ============================================
// 2. INFRASTRUCTURE SERVICES
// ============================================

var redis = builder.AddRedis("redis")
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

// Identity Service
var identityService = builder.AddProject<Projects.IdentityAPI>("identityservice")
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

builder.Build().Run();