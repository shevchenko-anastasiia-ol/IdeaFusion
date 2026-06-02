using Collaboration.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure;

public class MongoDbContextTests
{
    private static IOptions<MongoDbSettings> CreateSettings(string dbName = "TestDb") =>
        Options.Create(new MongoDbSettings
        {
            ConnectionString    = "mongodb://localhost:27017",
            DatabaseName        = dbName,
            MaxConnectionPoolSize = 10,
            MinConnectionPoolSize = 1,
            ConnectTimeoutSeconds = 5,
            SocketTimeoutSeconds  = 5
        });

    [Fact]
    public void Constructor_WithValidSettings_InitializesDatabase()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.Database.Should().NotBeNull();
    }

    [Fact]
    public void Database_ReturnsCorrectDatabaseName()
    {
        var ctx = new MongoDbContext(CreateSettings("MyDb"));
        ctx.Database.DatabaseNamespace.DatabaseName.Should().Be("MyDb");
    }

    [Fact]
    public void Teams_ReturnsNonNullCollection()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.Teams.Should().NotBeNull();
    }

    [Fact]
    public void CollaborationRequests_ReturnsNonNullCollection()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.CollaborationRequests.Should().NotBeNull();
    }

    [Fact]
    public void GroupInvitations_ReturnsNonNullCollection()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.GroupInvitations.Should().NotBeNull();
    }

    [Fact]
    public void TeamPosts_ReturnsNonNullCollection()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.TeamPosts.Should().NotBeNull();
    }

    [Fact]
    public void Teams_CollectionNameIsTeams()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.Teams.CollectionNamespace.CollectionName.Should().Be("Teams");
    }

    [Fact]
    public void CollaborationRequests_CollectionNameIsCollaborationRequests()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.CollaborationRequests.CollectionNamespace.CollectionName.Should().Be("CollaborationRequests");
    }

    [Fact]
    public void GroupInvitations_CollectionNameIsGroupInvitations()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.GroupInvitations.CollectionNamespace.CollectionName.Should().Be("GroupInvitations");
    }

    [Fact]
    public void TeamPosts_CollectionNameIsTeamPosts()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.TeamPosts.CollectionNamespace.CollectionName.Should().Be("TeamPosts");
    }

    [Fact]
    public void MultipleCallsToTeams_ReturnCollectionsWithSameName()
    {
        var ctx = new MongoDbContext(CreateSettings());
        ctx.Teams.CollectionNamespace.CollectionName.Should().Be(
            ctx.Teams.CollectionNamespace.CollectionName);
    }
}