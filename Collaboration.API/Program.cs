using Collaboration.API;
using Collaboration.API.Notifications;
using Collaboration.Application.Commands.DirectMessage;
using MediatR.Pipeline;
using Collaboration.API.Controllers;
using Collaboration.API.Hubs;
using Collaboration.API.Middleware;
using Collaboration.API.Services;
using Collaboration.Application.Behaviors;
using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Commands.Team;
using Collaboration.Application.Commands.Vacancy;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Application.Queries.Vacancy;
using Collaboration.Application.Validators;
using Collaboration.Domain.Interfaces;
using Collaboration.Infrastructure.Data;
using Collaboration.Infrastructure.Indexes;
using Collaboration.Infrastructure.Mapping;
using Collaboration.Infrastructure.Repositories;
using Collaboration.Infrastructure.Seeding;
using CollaborationGrpcService.MappingProfiles;
using CollaborationGrpcService.Services;
using FluentValidation;
using GrpcClients.Interfaces;
using IdeaFusion.Grpc.Teams;
using IdeaFusion.Grpc.Users;
using IdeaFusion.GrpcClients.Clients;
using ServiceDefaults.Extensions;
using ServiceDefaults.Health;
using MongoDB.Driver;

// ВАЖЛИВО: має бути першим — до будь-якої ініціалізації MongoDB
MongoMappings.Register();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();
builder.Services.AddGrpcWithObservability(builder.Environment);
builder.Services.AddServiceDiscovery();

// після builder.Services.AddServiceDiscovery();

builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
    {
        // Aspire registers IdentityAPI's endpoints under the names "identity-http"/"identity-https",
        // so the plain "https://identityservice" never resolved (gRPC went to DNS and failed with
        // "No such host"). "_endpoint.service" picks the named endpoint. The env var Grpc__X is
        // read as the configuration key Grpc:X.
        var address = builder.Configuration["Grpc:IdentityService"] ?? "https://_identity-https.identityservice";
        o.Address = new Uri(address);
    })
    // IdentityAPI requires an authenticated caller for everything, gRPC included, and this client
    // never sent a token: every call came back 401 (names and email addresses never resolved).
    // The caller's own bearer token is forwarded, as the aggregator does for HTTP.
    .AddCallCredentials((_, metadata, services) =>
    {
        var authorization = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            metadata.Add("Authorization", authorization);
        return Task.CompletedTask;
    })
    .AddServiceDiscovery();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();
builder.Services.AddScoped<Collaboration.Application.Interfaces.Services.IUserSnapshotService, GrpcUserSnapshotService>();

// Who is acting. Scoped, because it reads the token off the current HttpContext.
builder.Services.AddScoped<ICallerContext, CallerContext>();

// Privileged actions recorded in Identity's audit trail.
builder.Services.AddScoped<ICollaborationAuditWriter, CollaborationAuditWriter>();

// --- MongoDB ---
var aspireConn = builder.Configuration.GetConnectionString("collaboration-db")
                ?? builder.Configuration.GetConnectionString("mongodb");

if (!string.IsNullOrEmpty(aspireConn))
{
    builder.Services.Configure<MongoDbSettings>(options =>
    {
        options.ConnectionString = aspireConn;
        options.DatabaseName = "collaboration-db";
        options.MaxConnectionPoolSize = 100;
        options.MinConnectionPoolSize = 5;
        options.ConnectTimeoutSeconds = 10;
        options.SocketTimeoutSeconds = 10;
    });
}
else
{
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection("MongoDbSettings"));
}

var mongoConnString = !string.IsNullOrEmpty(aspireConn)
    ? aspireConn
    : builder.Configuration.GetSection("MongoDbSettings").GetValue<string>("ConnectionString");

builder.Services.AddMongoDbTelemetry(mongoConnString!);

builder.Services.PostConfigure<MongoDbSettings>(options =>
{
    if (string.IsNullOrEmpty(options.ConnectionString))
        throw new InvalidOperationException("MongoDB ConnectionString is required");
    if (string.IsNullOrEmpty(options.DatabaseName))
        throw new InvalidOperationException("MongoDB DatabaseName is required");
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped(sp =>
{
    var context = sp.GetRequiredService<MongoDbContext>();
    return context.Database;
});

// --- Repositories ---
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ICollaborationRequestRepository, CollaborationRequestRepository>();
builder.Services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>();
builder.Services.AddScoped<ITeamPostRepository, TeamPostRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IDirectConversationRepository, DirectConversationRepository>();
builder.Services.AddScoped<ITeamVacancyRepository, TeamVacancyRepository>();
builder.Services.AddScoped<IVacancyApplicationRepository, VacancyApplicationRepository>();

// --- Infrastructure ---
builder.Services.AddSingleton<IIndexCreation, MongoIndexCreation>();
builder.Services.AddSingleton<IDataSeeder, DatabaseSeeder>();

// --- Auth ---
builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddIdeaFusionAuthorization();

// --- AutoMapper ---
builder.Services.AddAutoMapperWithLogging(
    typeof(CollaborationGrpcProfile).Assembly
);

// --- MediatR ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(BaseApiController).Assembly);
    cfg.RegisterServicesFromAssemblies(typeof(GetCollaborationRequestsByTeamQueryHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));

    // Email notifications: run after a command succeeded and only queue a letter.
    cfg.AddRequestPostProcessor<IRequestPostProcessor<CreateGroupInvitationCommand, Collaboration.Domain.Entities.GroupInvitation>, InvitationSentEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<AcceptGroupInvitationCommand, Collaboration.Domain.Entities.GroupInvitation>, InvitationAcceptedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<DeclineGroupInvitationCommand, Collaboration.Domain.Entities.GroupInvitation>, InvitationDeclinedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<CreateCollaborationRequestCommand, Collaboration.Domain.Entities.CollaborationRequest>, JoinRequestSentEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<AcceptCollaborationRequestCommand, Collaboration.Domain.Entities.CollaborationRequest>, JoinRequestAcceptedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<RejectCollaborationRequestCommand, Collaboration.Domain.Entities.CollaborationRequest>, JoinRequestRejectedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<ApplyToVacancyCommand, Collaboration.Domain.Entities.VacancyApplication>, ApplicationSentEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<AcceptVacancyApplicationCommand, Collaboration.Domain.Entities.VacancyApplication>, ApplicationAcceptedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<RejectVacancyApplicationCommand, Collaboration.Domain.Entities.VacancyApplication>, ApplicationRejectedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<SendDirectMessageCommand, DirectMessageSent>, DirectMessageEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<ChangeDirectConversationCommand, Collaboration.Domain.Entities.DirectConversation>, DirectRequestAcceptedEmail>();
    cfg.AddRequestPostProcessor<IRequestPostProcessor<Collaboration.Application.Commands.ChatMessage.SendChatMessageCommand, Collaboration.Domain.Entities.ChatMessage>, TeamChatMessageEmail>();
});

// --- Email notifications ---
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Section));
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSingleton<MessageEmailThrottle>();
builder.Services.AddHostedService<EmailDispatcher>();
builder.Services.AddScoped<INotificationSettingsRepository, NotificationSettingsRepository>();

// --- Validators ---

builder.Services.AddScoped<IValidator<CreateTeamVacancyCommand>, CreateTeamVacancyCommandValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamVacancyCommand>, UpdateTeamVacancyCommandValidator>();
builder.Services.AddScoped<IValidator<CloseTeamVacancyCommand>, CloseTeamVacancyCommandValidator>();
builder.Services.AddScoped<IValidator<ApplyToVacancyCommand>, ApplyToVacancyCommandValidator>();
builder.Services.AddScoped<IValidator<AcceptVacancyApplicationCommand>, AcceptVacancyApplicationCommandValidator>();
builder.Services.AddScoped<IValidator<RejectVacancyApplicationCommand>, RejectVacancyApplicationCommandValidator>();
builder.Services.AddScoped<IValidator<CancelVacancyApplicationCommand>, CancelVacancyApplicationCommandValidator>();
builder.Services.AddScoped<IValidator<SearchVacanciesQuery>, SearchVacanciesQueryValidator>();
builder.Services.AddScoped<IValidator<GetTeamVacanciesQuery>, GetTeamVacanciesQueryValidator>();
builder.Services.AddScoped<IValidator<GetVacancyApplicationsQuery>, GetVacancyApplicationsQueryValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamMemberPermissionsCommand>, UpdateTeamMemberPermissionsCommandValidator>();
builder.Services.AddScoped<IValidator<SetTeamMemberRoleCommand>, SetTeamMemberRoleCommandValidator>();

// Invitations, collaboration requests and membership: without these the pipeline let
// through invitations that then failed only when someone tried to accept them.
builder.Services.AddScoped<IValidator<CreateGroupInvitationCommand>, CreateGroupInvitationCommandValidator>();
builder.Services.AddScoped<IValidator<AcceptGroupInvitationCommand>, AcceptGroupInvitationCommandValidator>();
builder.Services.AddScoped<IValidator<DeclineGroupInvitationCommand>, DeclineGroupInvitationCommandValidator>();
builder.Services.AddScoped<IValidator<RevokeGroupInvitationCommand>, RevokeGroupInvitationCommandValidator>();
builder.Services.AddScoped<IValidator<CreateCollaborationRequestCommand>, CreateCollaborationRequestCommandValidator>();
builder.Services.AddScoped<IValidator<AcceptCollaborationRequestCommand>, AcceptCollaborationRequestCommandValidator>();
builder.Services.AddScoped<IValidator<RejectCollaborationRequestCommand>, RejectCollaborationRequestCommandValidator>();
builder.Services.AddScoped<IValidator<CancelCollaborationRequestCommand>, CancelCollaborationRequestCommandValidator>();
builder.Services.AddScoped<IValidator<AddTeamMemberCommand>, AddTeamMemberCommandValidator>();
builder.Services.AddScoped<IValidator<RemoveTeamMemberCommand>, RemoveTeamMemberCommandValidator>();
builder.Services.AddScoped<IValidator<AddRequiredRoleCommand>, AddRequiredRoleCommandValidator>();

// --- Memory Cache (для CachingBehavior) ---
builder.Services.AddMemoryCache();

// --- SignalR (чати команд) ---
builder.Services.AddSignalR();

// --- Controllers ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithKeycloak(builder.Configuration, "Collaboration API");

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddMongoHealthCheck(
        builder.Configuration,
        connectionName: "collaboration-db",
        serviceName: "collaborationservice",
        databaseName: "collaboration-db",
        timeoutSeconds: 5);

var app = builder.Build();

// --- MongoDB indexes + seeding ---
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Creating MongoDB indexes...");
        var indexService = scope.ServiceProvider.GetRequiredService<IIndexCreation>();
        await indexService.CreateIndexesAsync();
        logger.LogInformation("MongoDB indexes created successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating MongoDB indexes. Continuing anyway...");
    }

    try
    {
        logger.LogInformation("Starting database seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database seeding. Application will continue, but database may be empty.");
    }
}

// --- Middleware ---
app.UseSwaggerWithKeycloak();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapGrpcService<CollaborationRequestGrpcServiceImpl>();
app.MapGrpcServicesWithReflection();
app.MapHub<ChatHub>("/hubs/chat");

await app.RunAsync();

public partial class Program { }