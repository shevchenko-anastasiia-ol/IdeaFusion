using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Integration;

/// <summary>
/// CollaborationSnapshotController uses ContentDbContext directly (no BLL service),
/// so these tests exercise real InMemory EF operations end-to-end.
/// </summary>
public class CollaborationSnapshotIntegrationTests : IClassFixture<ContentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CollaborationSnapshotIntegrationTests(ContentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record EnsureRequest(string TeamName, string MongoTeamId, string? AvatarUrl = null);
    private record EnsureResponse(int CollaborationSnapshotId);

    // ── CREATE NEW ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_NewTeam_Returns200WithNewId()
    {
        var request = new EnsureRequest("Team Alpha", "mongo-id-alpha-001");

        var response = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EnsureResponse>();
        body!.CollaborationSnapshotId.Should().BeGreaterThan(0);
    }

    // ── IDEMPOTENCY BY EXTERNAL ID ────────────────────────────────────────────

    [Fact]
    public async Task Ensure_SameMongoId_ReturnsSameSnapshotId()
    {
        var request = new EnsureRequest("Team Beta", "mongo-id-beta-001");

        var first  = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure", request);
        var second = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var id1 = (await first.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;
        var id2 = (await second.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;

        id1.Should().Be(id2);
    }

    // ── UPDATE NAME IF CHANGED ────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_ExistingMongoId_UpdatesNameAndReturnsSameId()
    {
        var mongoId = "mongo-id-gamma-001";

        var first = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure",
            new EnsureRequest("Old Name", mongoId));
        var originalId = (await first.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;

        var updated = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure",
            new EnsureRequest("New Name", mongoId));

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedId = (await updated.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;
        updatedId.Should().Be(originalId);
    }

    // ── FALLBACK BY NAME ──────────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_SameName_DifferentMongoId_ReturnsSameSnapshotId()
    {
        var teamName = "Unique Team Delta";

        // First create by name+mongoId
        var first = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure",
            new EnsureRequest(teamName, "mongo-id-delta-001"));
        var originalId = (await first.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;

        // Same name but different MongoId — should match by name fallback
        var second = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure",
            new EnsureRequest(teamName, "mongo-id-delta-002"));

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondId = (await second.Content.ReadFromJsonAsync<EnsureResponse>())!.CollaborationSnapshotId;
        secondId.Should().Be(originalId);
    }

    // ── WITH AVATAR URL ───────────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_WithAvatarUrl_Returns200()
    {
        var request = new EnsureRequest(
            "Team Epsilon",
            "mongo-id-epsilon-001",
            "http://example.com/avatar.png");

        var response = await _client.PostAsJsonAsync("/api/collaborationsnapshot/ensure", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<EnsureResponse>())!
            .CollaborationSnapshotId.Should().BeGreaterThan(0);
    }
}
