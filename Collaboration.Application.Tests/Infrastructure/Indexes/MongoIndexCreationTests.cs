using System.Reflection;
using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Data;
using Collaboration.Infrastructure.Indexes;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Indexes;

public class MongoIndexCreationTests
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
    public void MongoIndexCreation_ImplementsIIndexCreation()
    {
        typeof(MongoIndexCreation).Should().Implement<IIndexCreation>();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        var context      = new MongoDbContext(CreateSettings());
        var indexCreation = new MongoIndexCreation(context);

        indexCreation.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContext_DoesNotThrowButStoredContextIsNull()
    {
        // MongoIndexCreation stores the context but does not null-check in the constructor.
        // Calling CreateIndexesAsync() later would throw when accessing _context.Teams etc.
        var indexCreation = new MongoIndexCreation(null!);
        indexCreation.Should().NotBeNull();
    }

    // ── CreateIndexesAsync signature ──────────────────────────────────────────

    [Fact]
    public void CreateIndexesAsync_MethodExists_WithCancellationTokenParameter()
    {
        var method = typeof(MongoIndexCreation).GetMethod(nameof(MongoIndexCreation.CreateIndexesAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().ContainSingle(p => p.ParameterType == typeof(CancellationToken));
    }

    // ── IIndexCreation contract ───────────────────────────────────────────────

    [Fact]
    public void IIndexCreation_DefinesCreateIndexesAsync()
    {
        var method = typeof(IIndexCreation).GetMethod(nameof(IIndexCreation.CreateIndexesAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    // ── CreateIndexesAsync – execution with mocked collections ───────────────

    [Fact]
    public async Task CreateIndexesAsync_WithMockedCollections_CreatesAllIndexesSuccessfully()
    {
        var dbMock   = new Mock<IMongoDatabase>();

        var teamsIdx = SetupIndexedCollection<Team>(dbMock, "Teams");
        var reqIdx   = SetupIndexedCollection<CollaborationRequest>(dbMock, "CollaborationRequests");
        var invIdx   = SetupIndexedCollection<GroupInvitation>(dbMock, "GroupInvitations");
        var postIdx  = SetupIndexedCollection<TeamPost>(dbMock, "TeamPosts");

        var context = new MongoDbContext(CreateSettings());
        typeof(MongoDbContext)
            .GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(context, dbMock.Object);

        var indexCreation = new MongoIndexCreation(context);
        await indexCreation.CreateIndexesAsync();

        teamsIdx.Verify(m => m.CreateOneAsync(
            It.IsAny<CreateIndexModel<Team>>(),
            It.IsAny<CreateOneIndexOptions?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(8));

        reqIdx.Verify(m => m.CreateOneAsync(
            It.IsAny<CreateIndexModel<CollaborationRequest>>(),
            It.IsAny<CreateOneIndexOptions?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(6));

        invIdx.Verify(m => m.CreateOneAsync(
            It.IsAny<CreateIndexModel<GroupInvitation>>(),
            It.IsAny<CreateOneIndexOptions?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(7));

        postIdx.Verify(m => m.CreateOneAsync(
            It.IsAny<CreateIndexModel<TeamPost>>(),
            It.IsAny<CreateOneIndexOptions?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    private static Mock<IMongoIndexManager<T>> SetupIndexedCollection<T>(
        Mock<IMongoDatabase> dbMock, string name)
    {
        var colMock = new Mock<IMongoCollection<T>>();
        var idxMock = new Mock<IMongoIndexManager<T>>();

        idxMock.Setup(m => m.CreateOneAsync(
                It.IsAny<CreateIndexModel<T>>(),
                It.IsAny<CreateOneIndexOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("index");

        colMock.SetupGet(c => c.Indexes).Returns(idxMock.Object);

        dbMock.Setup(d => d.GetCollection<T>(name, It.IsAny<MongoCollectionSettings?>()))
              .Returns(colMock.Object);

        return idxMock;
    }
}