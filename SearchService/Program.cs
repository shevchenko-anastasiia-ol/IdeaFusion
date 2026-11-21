using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using Npgsql;
using SearchService.Data;
using SearchService.Feed;
using SearchService.Indexing;
using SearchService.Personal;
using SearchService.Search;
using ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOpenTelemetryTracing();
builder.Services.AddCorrelationIdForwarding();

builder.Services
    .AddKeycloakAuthentication(builder.Configuration)
    .AddIdeaFusionAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // "new", "popular", "all" in bodies and responses, the same words as in the query string.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithKeycloak(builder.Configuration, "Search API");

string Required(string name) =>
    builder.Configuration.GetConnectionString(name)
    ?? throw new InvalidOperationException($"Connection string '{name}' is missing");

// searchdb is the only database written to; the sources are opened read-only.
var indexDb = SearchDatabase.IndexSource(Required("searchdb"));
builder.Services.AddSingleton(indexDb);
var contentDb = SearchDatabase.ReadOnly(Required("contentdb"));
builder.Services.AddSingleton<ISearchSource>(new ContentPostSource(contentDb));
builder.Services.AddSingleton<IInteractionSource>(new ContentInteractionSource(contentDb));
builder.Services.AddSingleton<ISearchSource>(new IdentityUserSource(SearchDatabase.ReadOnly(Required("identityservicedb"))));

var collaboration = new MongoClient(Required("collaboration-db"))
    .GetDatabase(builder.Configuration["Search:CollaborationDatabase"] ?? "collaboration-db");
builder.Services.AddSingleton<ISearchSource>(new TeamSource(collaboration));
builder.Services.AddSingleton<ISearchSource>(new VacancySource(collaboration));

builder.Services.Configure<SearchIndexOptions>(builder.Configuration.GetSection("Search"));
builder.Services.AddSingleton<IndexSyncTrigger>();
builder.Services.AddSingleton<SearchIndexWriter>();
builder.Services.AddSingleton<TagCatalogCache>();
builder.Services.AddSingleton<SearchQueryService>();
builder.Services.AddSingleton<PersonalSearchStore>();
builder.Services.AddSingleton<SubscriptionMatcher>();
builder.Services.AddSingleton<InterestProfileWriter>();
builder.Services.AddSingleton<FeedService>();
builder.Services.AddSingleton<FeedEventLog>();
builder.Services.AddSingleton<SearchMetrics>();
builder.Services.AddHostedService<IndexSyncService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(Required("searchdb"), name: "searchdb", tags: ["db", "ready"]);

var app = builder.Build();

app.UseSwaggerWithKeycloak();
app.UseCorrelationId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program { }
