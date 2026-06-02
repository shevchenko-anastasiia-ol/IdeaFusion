using ContentAPI.Controllers;
using ContentDAL.Data;
using ContentDomain.Entity;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Content.Tests.Controllers;

public class CollaborationSnapshotControllerTests : IDisposable
{
    private readonly ContentDbContext _db;
    private readonly CollaborationSnapshotController _controller;

    public CollaborationSnapshotControllerTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ContentDbContext(options);
        _controller = new CollaborationSnapshotController(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<CollaborationSnapshot> SeedSnapshot(
        string externalId, string name, string? avatarUrl = null, int collaborationId = 100_000_001)
    {
        var snapshot = new CollaborationSnapshot
        {
            CollaborationId = collaborationId,
            ExternalId      = externalId,
            Name            = name,
            AvatarUrl       = avatarUrl,
            SyncedAt        = DateTime.UtcNow.AddDays(-1)
        };
        _db.CollaborationSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();
        return snapshot;
    }

    // ── Ensure – found by ExternalId, no changes ──────────────────────────────

    [Fact]
    public async Task Ensure_FoundByExternalId_NoChanges_ReturnsExistingId()
    {
        var seeded  = await SeedSnapshot("ext-1", "Team Alpha");
        var request = new EnsureCollaborationSnapshotRequest("Team Alpha", "ext-1");

        var result = await _controller.Ensure(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((EnsureCollaborationSnapshotResponse)ok.Value!).CollaborationSnapshotId
            .Should().Be(seeded.CollaborationSnapshotId);
    }

    [Fact]
    public async Task Ensure_FoundByExternalId_NoChanges_DoesNotCallSaveChanges()
    {
        await SeedSnapshot("ext-2", "Team Beta", "https://avatar.jpg");
        var request = new EnsureCollaborationSnapshotRequest("Team Beta", "ext-2", "https://avatar.jpg");

        await _controller.Ensure(request, CancellationToken.None);

        // Verify nothing changed — record count is still 1
        var count = await _db.CollaborationSnapshots.CountAsync();
        count.Should().Be(1);
    }

    // ── Ensure – found by ExternalId, fields changed ──────────────────────────

    [Fact]
    public async Task Ensure_FoundByExternalId_NameChanged_UpdatesName()
    {
        var seeded  = await SeedSnapshot("ext-3", "Old Name");
        var request = new EnsureCollaborationSnapshotRequest("New Name", "ext-3");

        await _controller.Ensure(request, CancellationToken.None);

        var updated = await _db.CollaborationSnapshots.FindAsync(seeded.CollaborationSnapshotId);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Ensure_FoundByExternalId_AvatarChanged_UpdatesAvatar()
    {
        var seeded  = await SeedSnapshot("ext-4", "Team Delta");
        var request = new EnsureCollaborationSnapshotRequest("Team Delta", "ext-4", "https://new-avatar.png");

        await _controller.Ensure(request, CancellationToken.None);

        var updated = await _db.CollaborationSnapshots.FindAsync(seeded.CollaborationSnapshotId);
        updated!.AvatarUrl.Should().Be("https://new-avatar.png");
    }

    [Fact]
    public async Task Ensure_FoundByExternalId_NameChanged_ReturnsExistingId()
    {
        var seeded  = await SeedSnapshot("ext-5", "Old Name");
        var request = new EnsureCollaborationSnapshotRequest("Renamed Team", "ext-5");

        var result = await _controller.Ensure(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((EnsureCollaborationSnapshotResponse)ok.Value!).CollaborationSnapshotId
            .Should().Be(seeded.CollaborationSnapshotId);
    }

    // ── Ensure – found by Name fallback ───────────────────────────────────────

    [Fact]
    public async Task Ensure_FoundByName_SetsExternalId()
    {
        var seeded  = await SeedSnapshot(null!, "Team Echo", collaborationId: 100_000_002);
        var request = new EnsureCollaborationSnapshotRequest("Team Echo", "new-ext-6");

        await _controller.Ensure(request, CancellationToken.None);

        var updated = await _db.CollaborationSnapshots.FindAsync(seeded.CollaborationSnapshotId);
        updated!.ExternalId.Should().Be("new-ext-6");
    }

    [Fact]
    public async Task Ensure_FoundByName_ReturnsExistingId()
    {
        var seeded  = await SeedSnapshot(null!, "Team Foxtrot", collaborationId: 100_000_003);
        var request = new EnsureCollaborationSnapshotRequest("Team Foxtrot", "ext-7");

        var result = await _controller.Ensure(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((EnsureCollaborationSnapshotResponse)ok.Value!).CollaborationSnapshotId
            .Should().Be(seeded.CollaborationSnapshotId);
    }

    [Fact]
    public async Task Ensure_FoundByName_DoesNotOverwriteExistingAvatar()
    {
        var seeded  = await SeedSnapshot(null!, "Team Golf", "https://existing-avatar.jpg", 100_000_004);
        var request = new EnsureCollaborationSnapshotRequest("Team Golf", "ext-8");

        await _controller.Ensure(request, CancellationToken.None);

        var updated = await _db.CollaborationSnapshots.FindAsync(seeded.CollaborationSnapshotId);
        updated!.AvatarUrl.Should().Be("https://existing-avatar.jpg");
    }

    // ── Ensure – new snapshot created ─────────────────────────────────────────

    [Fact]
    public async Task Ensure_NotFound_CreatesNewSnapshot()
    {
        var request = new EnsureCollaborationSnapshotRequest("Brand New Team", "ext-brand-new", "https://img.png");

        await _controller.Ensure(request, CancellationToken.None);

        var saved = await _db.CollaborationSnapshots
            .FirstOrDefaultAsync(c => c.ExternalId == "ext-brand-new");
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Brand New Team");
        saved.AvatarUrl.Should().Be("https://img.png");
    }

    [Fact]
    public async Task Ensure_NotFound_ReturnsNewSnapshotId()
    {
        var request = new EnsureCollaborationSnapshotRequest("Another Team", "ext-another");

        var result = await _controller.Ensure(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((EnsureCollaborationSnapshotResponse)ok.Value!).CollaborationSnapshotId
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Ensure_NotFound_CollaborationIdIsInExpectedRange()
    {
        var request = new EnsureCollaborationSnapshotRequest("Range Team", "ext-range");

        await _controller.Ensure(request, CancellationToken.None);

        var saved = await _db.CollaborationSnapshots
            .FirstAsync(c => c.ExternalId == "ext-range");
        saved.CollaborationId.Should().BeInRange(100_000_000, 999_999_999);
    }

    // ── Ensure – idempotency ───────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_CalledTwiceWithSameMongoId_ReturnsSameSnapshotId()
    {
        var request = new EnsureCollaborationSnapshotRequest("Idempotent Team", "ext-idem");

        var r1 = await _controller.Ensure(request, CancellationToken.None);
        var r2 = await _controller.Ensure(request, CancellationToken.None);

        var id1 = ((EnsureCollaborationSnapshotResponse)((OkObjectResult)r1.Result!).Value!).CollaborationSnapshotId;
        var id2 = ((EnsureCollaborationSnapshotResponse)((OkObjectResult)r2.Result!).Value!).CollaborationSnapshotId;
        id1.Should().Be(id2);
    }

    [Fact]
    public async Task Ensure_CalledTwiceWithSameMongoId_OnlyOneRecordExists()
    {
        var request = new EnsureCollaborationSnapshotRequest("Single Team", "ext-single");

        await _controller.Ensure(request, CancellationToken.None);
        await _controller.Ensure(request, CancellationToken.None);

        var count = await _db.CollaborationSnapshots.CountAsync(c => c.ExternalId == "ext-single");
        count.Should().Be(1);
    }

    // ── Ensure – collision resolution ─────────────────────────────────────────

    [Fact]
    public async Task Ensure_CollisionOnCollaborationId_IncrementsToFreeSlot()
    {
        var request = new EnsureCollaborationSnapshotRequest("Collision Team", "ext-collision");

        // Pre-occupy the expected CollaborationId
        var expectedId = (int)(Math.Abs((long)"ext-collision".GetHashCode()) % 900_000_000) + 100_000_000;
        _db.CollaborationSnapshots.Add(new CollaborationSnapshot
        {
            CollaborationId = expectedId,
            ExternalId      = "other-team",
            Name            = "Other Team",
            SyncedAt        = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _controller.Ensure(request, CancellationToken.None);

        var created = await _db.CollaborationSnapshots
            .FirstAsync(c => c.ExternalId == "ext-collision");
        created.CollaborationId.Should().Be(expectedId + 1);
    }
}
