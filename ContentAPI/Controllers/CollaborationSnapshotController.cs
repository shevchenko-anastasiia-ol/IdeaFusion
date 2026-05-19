using ContentDAL.Data;
using ContentDomain.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.Controllers;

public record EnsureCollaborationSnapshotRequest(
    string TeamName,
    string MongoTeamId,
    string? AvatarUrl = null);

public record EnsureCollaborationSnapshotResponse(int CollaborationSnapshotId);

[ApiController]
[Route("api/[controller]")]
public class CollaborationSnapshotController : ControllerBase
{
    private readonly ContentDbContext _db;

    public CollaborationSnapshotController(ContentDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get or create a CollaborationSnapshot for a given team.
    /// Idempotent: returns the existing snapshot if one matches by MongoTeamId or TeamName.
    /// </summary>
    [HttpPost("ensure")]
    [ProducesResponseType(typeof(EnsureCollaborationSnapshotResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnsureCollaborationSnapshotResponse>> Ensure(
        [FromBody] EnsureCollaborationSnapshotRequest request,
        CancellationToken ct)
    {
        // Find by ExternalId (MongoDB team ID) first — most precise match
        var existing = await _db.CollaborationSnapshots
            .FirstOrDefaultAsync(c => c.ExternalId == request.MongoTeamId, ct);

        if (existing != null)
        {
            // Update name/avatar if changed
            if (existing.Name != request.TeamName || existing.AvatarUrl != request.AvatarUrl)
            {
                existing.Name = request.TeamName;
                existing.AvatarUrl = request.AvatarUrl;
                existing.SyncedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return Ok(new EnsureCollaborationSnapshotResponse(existing.CollaborationSnapshotId));
        }

        // Fall back to matching by name
        existing = await _db.CollaborationSnapshots
            .FirstOrDefaultAsync(c => c.Name == request.TeamName, ct);

        if (existing != null)
        {
            existing.ExternalId = request.MongoTeamId;
            existing.AvatarUrl ??= request.AvatarUrl;
            existing.SyncedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Ok(new EnsureCollaborationSnapshotResponse(existing.CollaborationSnapshotId));
        }

        // Deterministic numeric CollaborationId derived from MongoDB team ID
        var collabId = (int)(Math.Abs((long)request.MongoTeamId.GetHashCode()) % 900_000_000) + 100_000_000;

        // Resolve collision if needed
        var takenIds = await _db.CollaborationSnapshots
            .Where(c => c.CollaborationId >= collabId && c.CollaborationId <= collabId + 9)
            .Select(c => c.CollaborationId)
            .ToListAsync(ct);
        while (takenIds.Contains(collabId)) collabId++;

        var snapshot = new CollaborationSnapshot
        {
            CollaborationId = collabId,
            ExternalId = request.MongoTeamId,
            Name = request.TeamName,
            AvatarUrl = request.AvatarUrl,
            SyncedAt = DateTime.UtcNow,
        };

        _db.CollaborationSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        return Ok(new EnsureCollaborationSnapshotResponse(snapshot.CollaborationSnapshotId));
    }
}