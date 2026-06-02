using Collaboration.Domain.Common;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure;

public class GenericRepositoryTests
{
    private readonly Mock<IMongoDatabase> _dbMock;
    private readonly Mock<IMongoCollection<Domain.Entities.Team>> _collectionMock;
    private readonly GenericRepository<Domain.Entities.Team> _repo;

    public GenericRepositoryTests()
    {
        _collectionMock = new Mock<IMongoCollection<Domain.Entities.Team>>();
        _dbMock = new Mock<IMongoDatabase>();
        _dbMock.Setup(d => d.GetCollection<Domain.Entities.Team>(
                It.IsAny<string>(),
                It.IsAny<MongoCollectionSettings?>()))
            .Returns(_collectionMock.Object);

        _repo = new GenericRepository<Domain.Entities.Team>(_dbMock.Object);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullDatabase_ThrowsArgumentNullException()
    {
        var act = () => new GenericRepository<Domain.Entities.Team>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithCustomCollectionName_UsesProvidedName()
    {
        var repo = new GenericRepository<Domain.Entities.Team>(_dbMock.Object, "custom_teams");
        _dbMock.Verify(d => d.GetCollection<Domain.Entities.Team>("custom_teams", null), Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WithEmptyOrWhitespaceId_ReturnsNullWithoutDbCall(string id)
    {
        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
        _collectionMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
            It.IsAny<FindOptions<Domain.Entities.Team, Domain.Entities.Team>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAsync_WithEmptyOrWhitespaceId_ReturnsFalseWithoutDbCall(string id)
    {
        var result = await _repo.DeleteAsync(id);

        result.Should().BeFalse();
        _collectionMock.Verify(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_WithEmptyOrWhitespaceId_ReturnsFalseWithoutDbCall(string id)
    {
        var result = await _repo.ExistsAsync(id);

        result.Should().BeFalse();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        var act = async () => await _repo.CreateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        var act = async () => await _repo.UpdateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithEntityWithEmptyId_ThrowsArgumentException()
    {
        var owner = new UserSnapshot("u1", "alice");
        var team = new Domain.Entities.Team("T", "desc", "Design", [], owner);

        // Force empty Id via reflection
        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
            .SetValue(team, "");

        var act = async () => await _repo.UpdateAsync(team);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Id*");
    }

    // ── CreateManyAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateManyAsync_WithEmptyList_ReturnsEmptyWithoutDbCall()
    {
        var result = await _repo.CreateManyAsync([]);

        result.Should().BeEmpty();
        _collectionMock.Verify(c => c.InsertManyAsync(
            It.IsAny<IEnumerable<Domain.Entities.Team>>(),
            It.IsAny<InsertManyOptions?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_WithNullCollection_ReturnsEmptyWithoutDbCall()
    {
        var result = await _repo.CreateManyAsync(null!);

        result.Should().BeEmpty();
    }

    // ── CreateAsync – inserts and returns entity ──────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidEntity_CallsInsertOneAndReturnsEntity()
    {
        _collectionMock
            .Setup(c => c.InsertOneAsync(
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<InsertOneOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("New Team", "desc", "Design", [], owner);

        var result = await _repo.CreateAsync(team);

        result.Should().BeSameAs(team);
        _collectionMock.Verify(c => c.InsertOneAsync(
            team, It.IsAny<InsertOneOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateAsync – replace and return ─────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenReplaceSucceeds_ReturnsEntity()
    {
        var replaceResult = new Mock<ReplaceOneResult>();
        replaceResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        replaceResult.SetupGet(r => r.ModifiedCount).Returns(1);
        _collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResult.Object);

        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("Existing", "desc", "Design", [], owner);

        var result = await _repo.UpdateAsync(team);
        result.Should().BeSameAs(team);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentNotFound_ThrowsInvalidOperationException()
    {
        var replaceResult = new Mock<ReplaceOneResult>();
        replaceResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        replaceResult.SetupGet(r => r.ModifiedCount).Returns(0);
        _collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResult.Object);

        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("Ghost", "desc", "Design", [], owner);

        await _repo.Invoking(r => r.UpdateAsync(team))
                   .Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("*Failed to update*");
    }

    // ── DeleteAsync – successful delete ──────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenDocumentDeleted_ReturnsTrue()
    {
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        deleteResult.SetupGet(r => r.DeletedCount).Returns(1);
        _collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult.Object);

        var result = await _repo.DeleteAsync("some-valid-id");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenNothingDeleted_ReturnsFalse()
    {
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        deleteResult.SetupGet(r => r.DeletedCount).Returns(0);
        _collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult.Object);

        var result = await _repo.DeleteAsync("missing-id");
        result.Should().BeFalse();
    }

    // ── ExistsAsync – count-based ─────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_WhenCountGreaterThanZero_ReturnsTrue()
    {
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await _repo.ExistsAsync("exists-id");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenCountIsZero_ReturnsFalse()
    {
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await _repo.ExistsAsync("missing-id");
        result.Should().BeFalse();
    }

    // ── CreateManyAsync – items ───────────────────────────────────────────────

    [Fact]
    public async Task CreateManyAsync_WithItems_CallsInsertManyAndReturnsAll()
    {
        _collectionMock
            .Setup(c => c.InsertManyAsync(
                It.IsAny<IEnumerable<Domain.Entities.Team>>(),
                It.IsAny<InsertManyOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var owner = new UserSnapshot("u1", "alice");
        var teams = new List<Domain.Entities.Team>
        {
            new("Team A", "desc", "Design", [], owner),
            new("Team B", "desc", "Design", [], owner)
        };

        var result = await _repo.CreateManyAsync(teams);
        result.Should().HaveCount(2);
    }

    // ── TryUpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task TryUpdateAsync_WhenDbThrows_ReturnsFalseAndNull()
    {
        _collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("connection error"));

        var owner        = new UserSnapshot("u1", "alice");
        var team         = new Domain.Entities.Team("T", "desc", "Design", [], owner);
        var (ok, entity) = await _repo.TryUpdateAsync(team);

        ok.Should().BeFalse();
        entity.Should().BeNull();
    }

    [Fact]
    public async Task TryUpdateAsync_WithNoVersionProperty_WhenUpdateSucceeds_ReturnsTrueAndEntity()
    {
        var replaceResult = new Mock<ReplaceOneResult>();
        replaceResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        replaceResult.SetupGet(r => r.ModifiedCount).Returns(1);
        _collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResult.Object);

        var owner        = new UserSnapshot("u1", "alice");
        var team         = new Domain.Entities.Team("T", "desc", "Design", [], owner);
        var (ok, entity) = await _repo.TryUpdateAsync(team);

        ok.Should().BeTrue();
        entity.Should().BeSameAs(team);
    }

    // ── UpdateWithConcurrencyCheckAsync – no version ──────────────────────────

    [Fact]
    public async Task UpdateWithConcurrencyCheckAsync_WithNoVersionProperty_DelegatesToUpdateAsync()
    {
        var replaceResult = new Mock<ReplaceOneResult>();
        replaceResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        replaceResult.SetupGet(r => r.ModifiedCount).Returns(1);
        _collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<Domain.Entities.Team>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResult.Object);

        var owner  = new UserSnapshot("u1", "alice");
        var team   = new Domain.Entities.Team("T", "desc", "Design", [], owner);
        var result = await _repo.UpdateWithConcurrencyCheckAsync(team);

        result.Should().BeSameAs(team);
    }

    // ── GetByIdAsync – valid id ───────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSingleEntity()
    {
        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("T", "desc", "Design", [], owner);
        SetupFindAsync([team]);

        var result = await _repo.GetByIdAsync("some-id");

        result.Should().BeSameAs(team);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsAll()
    {
        var owner = new UserSnapshot("u1", "alice");
        var teams = new List<Domain.Entities.Team>
        {
            new("A", "desc", "Design", [], owner),
            new("B", "desc", "Design", [], owner)
        };
        SetupFindAsync(teams);

        var result = (await _repo.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
    }

    // ── FindAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FindAsync_WithFilter_ReturnsMatchingItems()
    {
        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("Match", "desc", "Design", [], owner);
        SetupFindAsync([team]);

        var result = (await _repo.FindAsync(t => t.Name == "Match")).ToList();

        result.Should().ContainSingle();
    }

    // ── FindOneAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FindOneAsync_WithFilter_ReturnsFirstMatch()
    {
        var owner = new UserSnapshot("u1", "alice");
        var team  = new Domain.Entities.Team("First", "desc", "Design", [], owner);
        SetupFindAsync([team]);

        var result = await _repo.FindOneAsync(t => t.Name == "First");

        result.Should().BeSameAs(team);
    }

    // ── CountAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsCount()
    {
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5L);

        var count = await _repo.CountAsync(t => !t.IsDeleted);

        count.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_WithNullFilter_ReturnsCount()
    {
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3L);

        var count = await _repo.CountAsync();

        count.Should().Be(3);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_WithBasicParams_ReturnsPaged()
    {
        var owner = new UserSnapshot("u1", "alice");
        var teams = new List<Domain.Entities.Team>
        {
            new("T1", "desc", "Design", [], owner)
        };
        SetupFindAsync(teams);
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var (items, total) = await _repo.GetPagedAsync(1, 10);

        items.Should().HaveCount(1);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithOrderByDescending_ReturnsPaged()
    {
        var owner = new UserSnapshot("u1", "alice");
        SetupFindAsync([new("T1", "desc", "Design", [], owner)]);
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var (items, total) = await _repo.GetPagedAsync(1, 10, orderBy: t => t.CreatedAt, ascending: false);

        items.Should().HaveCount(1);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithCustomOrderByAscending_ReturnsPaged()
    {
        var owner = new UserSnapshot("u1", "alice");
        SetupFindAsync([new("T1", "desc", "Design", [], owner)]);
        _collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var (items, _) = await _repo.GetPagedAsync(1, 10, orderBy: t => t.CreatedAt, ascending: true);

        items.Should().HaveCount(1);
    }

    // ── DeleteManyAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteManyAsync_WhenDeleted_ReturnsTrue()
    {
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        deleteResult.SetupGet(r => r.DeletedCount).Returns(3);
        _collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult.Object);

        var result = await _repo.DeleteManyAsync(t => t.IsDeleted);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteManyAsync_WhenNothingDeleted_ReturnsFalse()
    {
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        deleteResult.SetupGet(r => r.DeletedCount).Returns(0);
        _collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult.Object);

        var result = await _repo.DeleteManyAsync(t => t.IsDeleted);

        result.Should().BeFalse();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetupFindAsync(List<Domain.Entities.Team> items)
    {
        var cursor = new Mock<IAsyncCursor<Domain.Entities.Team>>();
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(items.Count > 0)
              .ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(items);

        _collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Domain.Entities.Team>>(),
                It.IsAny<FindOptions<Domain.Entities.Team, Domain.Entities.Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
    }
}

// ── Versioned-entity tests ────────────────────────────────────────────────────

public class GenericRepositoryVersionedTests
{
    public class VersionedEntity : BaseEntity
    {
        public int? Version { get; set; }
    }

    private readonly Mock<IMongoDatabase>              _dbMock;
    private readonly Mock<IMongoCollection<VersionedEntity>> _colMock;
    private readonly GenericRepository<VersionedEntity>     _repo;

    public GenericRepositoryVersionedTests()
    {
        _colMock = new Mock<IMongoCollection<VersionedEntity>>();
        _dbMock  = new Mock<IMongoDatabase>();
        _dbMock.Setup(d => d.GetCollection<VersionedEntity>(
                It.IsAny<string>(),
                It.IsAny<MongoCollectionSettings?>()))
            .Returns(_colMock.Object);

        _repo = new GenericRepository<VersionedEntity>(_dbMock.Object);
    }

    private VersionedEntity CreateEntity(int? version = 1)
    {
        var e = new VersionedEntity { Version = version };
        return e;
    }

    private void SetupReplaceResult(bool acknowledged, long modifiedCount)
    {
        var result = new Mock<ReplaceOneResult>();
        result.SetupGet(r => r.IsAcknowledged).Returns(acknowledged);
        result.SetupGet(r => r.ModifiedCount).Returns(modifiedCount);
        _colMock.Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<VersionedEntity>>(),
                It.IsAny<VersionedEntity>(),
                It.IsAny<ReplaceOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);
    }

    // ── UpdateWithConcurrencyCheckAsync ───────────────────────────────────────

    [Fact]
    public async Task UpdateWithConcurrencyCheckAsync_WhenVersionNull_ThrowsInvalidOperationException()
    {
        var entity = CreateEntity(null);

        await _repo.Invoking(r => r.UpdateWithConcurrencyCheckAsync(entity))
                   .Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("*Version property is null*");
    }

    [Fact]
    public async Task UpdateWithConcurrencyCheckAsync_WhenVersionValid_IncrementsAndReturnsEntity()
    {
        var entity = CreateEntity(5);
        SetupReplaceResult(true, 1);

        var result = await _repo.UpdateWithConcurrencyCheckAsync(entity);

        result.Should().BeSameAs(entity);
        entity.Version.Should().Be(6);
    }

    [Fact]
    public async Task UpdateWithConcurrencyCheckAsync_WhenVersionMismatch_RevertsVersionAndThrows()
    {
        var entity = CreateEntity(3);
        SetupReplaceResult(true, 0);

        await _repo.Invoking(r => r.UpdateWithConcurrencyCheckAsync(entity))
                   .Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("*Concurrency conflict*");

        entity.Version.Should().Be(3);
    }

    // ── TryUpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task TryUpdateAsync_WhenVersionNull_ReturnsFalseNull()
    {
        var entity       = CreateEntity(null);
        var (ok, result) = await _repo.TryUpdateAsync(entity);

        ok.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryUpdateAsync_WhenVersionValidAndSucceeds_ReturnsTrueAndEntity()
    {
        var entity = CreateEntity(2);
        SetupReplaceResult(true, 1);

        var (ok, result) = await _repo.TryUpdateAsync(entity);

        ok.Should().BeTrue();
        result.Should().BeSameAs(entity);
        entity.Version.Should().Be(3);
    }

    [Fact]
    public async Task TryUpdateAsync_WhenVersionMismatch_RevertsVersionAndReturnsFalse()
    {
        var entity = CreateEntity(7);
        SetupReplaceResult(true, 0);

        var (ok, result) = await _repo.TryUpdateAsync(entity);

        ok.Should().BeFalse();
        result.Should().BeNull();
        entity.Version.Should().Be(7);
    }
}