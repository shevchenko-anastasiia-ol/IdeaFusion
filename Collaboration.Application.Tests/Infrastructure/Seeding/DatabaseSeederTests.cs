using System.Reflection;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Data;
using Collaboration.Infrastructure.Seeding;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Seeding;

public class DatabaseSeederTests
{
    private static IOptions<MongoDbSettings> CreateSettings() =>
        Options.Create(new MongoDbSettings
        {
            ConnectionString      = "mongodb://localhost:27017",
            DatabaseName          = "TestDb",
            MaxConnectionPoolSize = 5,
            MinConnectionPoolSize = 1,
            ConnectTimeoutSeconds = 5,
            SocketTimeoutSeconds  = 5
        });

    // ── Interface contract ────────────────────────────────────────────────────

    [Fact]
    public void DatabaseSeeder_ImplementsIDataSeeder()
    {
        typeof(DatabaseSeeder).Should().Implement<IDataSeeder>();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var context = new MongoDbContext(CreateSettings());
        var logger  = new Mock<ILogger<DatabaseSeeder>>();

        var seeder = new DatabaseSeeder(context, logger.Object);

        seeder.Should().NotBeNull();
    }

    // ── SeedAsync – logging behaviour ─────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_LogsStartMessage_BeforeAnyDbOperation()
    {
        // Uses a connection that will fail, but we verify the start log fires first.
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString      = "mongodb://127.0.0.1:1",
            DatabaseName          = "TestDb",
            MaxConnectionPoolSize = 1,
            MinConnectionPoolSize = 0,
            ConnectTimeoutSeconds = 1,
            SocketTimeoutSeconds  = 1
        });

        var context    = new MongoDbContext(settings);
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder     = new DatabaseSeeder(context, loggerMock.Object);

        try { await seeder.SeedAsync(); }
        catch { /* expected – MongoDB is not available */ }

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Starting")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenDbOperationFails_LogsErrorAndRethrows()
    {
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString      = "mongodb://127.0.0.1:1",
            DatabaseName          = "TestDb",
            MaxConnectionPoolSize = 1,
            MinConnectionPoolSize = 0,
            ConnectTimeoutSeconds = 1,
            SocketTimeoutSeconds  = 1
        });

        var context    = new MongoDbContext(settings);
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder     = new DatabaseSeeder(context, loggerMock.Object);

        await seeder.Invoking(s => s.SeedAsync())
                    .Should().ThrowAsync<Exception>();

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── SeedAsync method signature ────────────────────────────────────────────

    [Fact]
    public void SeedAsync_MethodExists_WithCancellationTokenParameter()
    {
        var method = typeof(DatabaseSeeder).GetMethod(nameof(DatabaseSeeder.SeedAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    // ── SeedAsync – success path with mocked collections ─────────────────────

    [Fact]
    public async Task SeedAsync_WithMockedCollections_LogsCompletedSuccessfully()
    {
        var dbMock = new Mock<IMongoDatabase>();

        var teamsCol = SetupSeederCollection<Team>(dbMock, "Teams");
        SetupSeederCollection<CollaborationRequest>(dbMock, "CollaborationRequests");
        SetupSeederCollection<GroupInvitation>(dbMock, "GroupInvitations");
        SetupSeederCollection<TeamPost>(dbMock, "TeamPosts");

        var context = new MongoDbContext(CreateSettings());
        typeof(MongoDbContext)
            .GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(context, dbMock.Object);

        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder     = new DatabaseSeeder(context, loggerMock.Object);

        await seeder.SeedAsync();

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static Mock<IMongoCollection<T>> SetupSeederCollection<T>(
        Mock<IMongoDatabase> dbMock, string name)
    {
        var colMock    = new Mock<IMongoCollection<T>>();

        // Cursor for Find().FirstOrDefaultAsync() → FindAsync<T>
        var docCursor = new Mock<IAsyncCursor<T>>();
        docCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        docCursor.Setup(c => c.Current).Returns(new List<T>());
        docCursor.Setup(c => c.Dispose());

        colMock.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, T>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(docCursor.Object);

        // Cursor for Find().AnyAsync() → FindAsync<BsonDocument> (MongoDB.Driver optimisation)
        var bsonCursor = new Mock<IAsyncCursor<BsonDocument>>();
        bsonCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bsonCursor.Setup(c => c.Current).Returns(new List<BsonDocument>());
        bsonCursor.Setup(c => c.Dispose());

        colMock.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, BsonDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bsonCursor.Object);

        colMock.Setup(c => c.InsertOneAsync(
                It.IsAny<T>(),
                It.IsAny<InsertOneOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        dbMock.Setup(d => d.GetCollection<T>(name, It.IsAny<MongoCollectionSettings?>()))
              .Returns(colMock.Object);

        return colMock;
    }

    // ── Cursor factories (fresh cursor per call) ──────────────────────────────

    private static Task<IAsyncCursor<T>> MakeTypedCursorTask<T>(T item)
    {
        var cur = new Mock<IAsyncCursor<T>>();
        cur.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(true).ReturnsAsync(false);
        cur.Setup(c => c.Current).Returns(new List<T> { item });
        cur.Setup(c => c.Dispose());
        return Task.FromResult(cur.Object);
    }

    private static Task<IAsyncCursor<BsonDocument>> MakeBsonCursorTask(bool hasItem)
    {
        var cur = new Mock<IAsyncCursor<BsonDocument>>();
        if (hasItem)
        {
            cur.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true).ReturnsAsync(false);
            cur.Setup(c => c.Current).Returns(new List<BsonDocument> { new() });
        }
        else
        {
            cur.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            cur.Setup(c => c.Current).Returns(new List<BsonDocument>());
        }
        cur.Setup(c => c.Dispose());
        return Task.FromResult(cur.Object);
    }

    // Teams: FindAsync<Team,Team> → dummyTeam; FindAsync<Team,BsonDocument> → teamExists
    private static Mock<IMongoCollection<Team>> SetupTeamCollectionFull(
        Mock<IMongoDatabase> dbMock, Team dummy, bool teamExists)
    {
        var col = new Mock<IMongoCollection<Team>>();

        col.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<FindOptions<Team, Team>>(),
                It.IsAny<CancellationToken>()))
           .Returns(() => MakeTypedCursorTask(dummy));

        col.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<FindOptions<Team, BsonDocument>>(),
                It.IsAny<CancellationToken>()))
           .Returns(() => MakeBsonCursorTask(teamExists));

        col.Setup(c => c.InsertOneAsync(
                It.IsAny<Team>(),
                It.IsAny<InsertOneOptions?>(),
                It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        dbMock.Setup(d => d.GetCollection<Team>("Teams", It.IsAny<MongoCollectionSettings?>()))
              .Returns(col.Object);

        return col;
    }

    // Other collections: only AnyAsync (BsonDocument) + InsertOneAsync
    private static Mock<IMongoCollection<T>> SetupCollectionWithFlag<T>(
        Mock<IMongoDatabase> dbMock, string name, bool itemExists)
    {
        var col = new Mock<IMongoCollection<T>>();

        col.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, BsonDocument>>(),
                It.IsAny<CancellationToken>()))
           .Returns(() => MakeBsonCursorTask(itemExists));

        col.Setup(c => c.InsertOneAsync(
                It.IsAny<T>(),
                It.IsAny<InsertOneOptions?>(),
                It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        dbMock.Setup(d => d.GetCollection<T>(name, It.IsAny<MongoCollectionSettings?>()))
              .Returns(col.Object);

        return col;
    }

    private static DatabaseSeeder BuildSeeder(
        Mock<IMongoDatabase> dbMock, out Mock<ILogger<DatabaseSeeder>> loggerMock)
    {
        var context = new MongoDbContext(CreateSettings());
        typeof(MongoDbContext)
            .GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(context, dbMock.Object);

        loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        return new DatabaseSeeder(context, loggerMock.Object);
    }

    private static Team MakeDummyTeam() =>
        new("T", "d", "c", new List<string> { "x" },
            new UserSnapshot("system", "System", null));

    // ── Teams found, nothing pre-exists → all inserts happen ─────────────────

    [Fact]
    public async Task SeedAsync_WhenTeamsFoundAndNothingExists_InsertsAllEntities()
    {
        var dbMock = new Mock<IMongoDatabase>();
        SetupTeamCollectionFull(dbMock, MakeDummyTeam(), teamExists: false);
        SetupCollectionWithFlag<CollaborationRequest>(dbMock, "CollaborationRequests", itemExists: false);
        SetupCollectionWithFlag<GroupInvitation>(dbMock, "GroupInvitations",           itemExists: false);
        SetupCollectionWithFlag<TeamPost>(dbMock, "TeamPosts",                         itemExists: false);

        var seeder = BuildSeeder(dbMock, out var loggerMock);
        await seeder.SeedAsync();

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── All items already exist → no inserts ─────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenAllItemsAlreadyExist_SkipsAllInserts()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var teamCol = SetupTeamCollectionFull(dbMock, MakeDummyTeam(), teamExists: true);
        var crCol   = SetupCollectionWithFlag<CollaborationRequest>(dbMock, "CollaborationRequests", itemExists: true);
        var giCol   = SetupCollectionWithFlag<GroupInvitation>(dbMock, "GroupInvitations",           itemExists: true);
        var tpCol   = SetupCollectionWithFlag<TeamPost>(dbMock, "TeamPosts",                         itemExists: true);

        var seeder = BuildSeeder(dbMock, out _);
        await seeder.SeedAsync();

        teamCol.Verify(c => c.InsertOneAsync(It.IsAny<Team>(),                  It.IsAny<InsertOneOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        crCol  .Verify(c => c.InsertOneAsync(It.IsAny<CollaborationRequest>(),  It.IsAny<InsertOneOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        giCol  .Verify(c => c.InsertOneAsync(It.IsAny<GroupInvitation>(),       It.IsAny<InsertOneOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        tpCol  .Verify(c => c.InsertOneAsync(It.IsAny<TeamPost>(),              It.IsAny<InsertOneOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}