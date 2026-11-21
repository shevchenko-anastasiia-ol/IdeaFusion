using FluentAssertions;
using IdentityBLL.DTOs;
using IdentityBLL.Services;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Identity.Tests.BLL;

public class AuditServiceTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _service = new AuditService(_context, new LoggerFactory().CreateLogger<AuditService>());
    }

    public void Dispose() => _context.Dispose();

    // ── RecordAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Record_PersistsTheEntryAndReturnsIt()
    {
        var actor = Guid.NewGuid();

        var result = await _service.RecordAsync(new AuditRecordDto
        {
            ActorId = actor,
            ActorName = "admin",
            Action = "user.role.grant",
            TargetType = "user",
            TargetId = "target-1",
            TargetName = "anna@ideafusion.com",
            Details = "{\"roles\":[\"moderator\"]}",
            Category = AuditCategory.User
        });

        result.Action.Should().Be("user.role.grant");
        result.ActorId.Should().Be(actor);
        result.Category.Should().Be("User");

        (await _context.AuditLogEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Record_StampsTheTimestampAsUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await _service.RecordAsync(New());

        result.CreatedAt.Should().BeOnOrAfter(before);
        result.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Record_TruncatesValuesThatWouldOverflowTheirColumn()
    {
        var result = await _service.RecordAsync(new AuditRecordDto
        {
            ActorId = Guid.NewGuid(),
            ActorName = new string('a', 400),
            Action = new string('b', 200),
            TargetType = new string('c', 100),
            TargetId = new string('d', 200),
            TargetName = new string('e', 400),
            Details = new string('f', 5000)
        });

        result.ActorName.Should().HaveLength(256);
        result.Action.Should().HaveLength(128);
        result.TargetType.Should().HaveLength(64);
        result.TargetId.Should().HaveLength(128);
        result.TargetName.Should().HaveLength(256);
        result.Details.Should().HaveLength(4000);
    }

    [Theory]
    [InlineData(null, "user", "id")]
    [InlineData("", "user", "id")]
    [InlineData("  ", "user", "id")]
    [InlineData("action", null, "id")]
    [InlineData("action", "", "id")]
    [InlineData("action", "user", null)]
    [InlineData("action", "user", "  ")]
    public async Task Record_RejectsAnEntryThatCannotBeAttributed(
        string? action, string? targetType, string? targetId)
    {
        var act = () => _service.RecordAsync(new AuditRecordDto
        {
            ActorId = Guid.NewGuid(),
            Action = action!,
            TargetType = targetType!,
            TargetId = targetId!
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsNewestFirst()
    {
        await SeedAsync("older", DateTime.UtcNow.AddHours(-1));
        await SeedAsync("newer", DateTime.UtcNow);

        var result = await _service.GetAsync(new AuditQueryDto());

        result.Select(e => e.Action).Should().BeEquivalentTo(["newer", "older"], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Get_FiltersByActor()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();

        await _service.RecordAsync(New(action: "mine", actorId: mine));
        await _service.RecordAsync(New(action: "theirs", actorId: theirs));

        var result = await _service.GetAsync(new AuditQueryDto { ActorId = mine });

        result.Should().ContainSingle();
        result[0].Action.Should().Be("mine");
    }

    [Fact]
    public async Task Get_FiltersByAction()
    {
        await _service.RecordAsync(New(action: "user.role.grant"));
        await _service.RecordAsync(New(action: "user.role.revoke"));

        var result = await _service.GetAsync(new AuditQueryDto { Action = "user.role.grant" });

        result.Should().ContainSingle();
        result[0].Action.Should().Be("user.role.grant");
    }

    [Fact]
    public async Task Get_FiltersByTargetType()
    {
        await _service.RecordAsync(New(action: "a", targetType: "user"));
        await _service.RecordAsync(New(action: "b", targetType: "post"));

        var result = await _service.GetAsync(new AuditQueryDto { TargetType = "post" });

        result.Should().ContainSingle();
        result[0].TargetType.Should().Be("post");
    }

    [Fact]
    public async Task Get_FiltersByTimeWindow()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);

        await SeedAsync("old", cutoff.AddHours(-1));
        await SeedAsync("new", DateTime.UtcNow);

        var result = await _service.GetAsync(new AuditQueryDto { From = cutoff });

        result.Should().ContainSingle();
        result[0].Action.Should().Be("new");
    }

    [Fact]
    public async Task Get_HonoursTheLimit()
    {
        for (var i = 0; i < 5; i++)
            await _service.RecordAsync(New(action: $"a{i}"));

        (await _service.GetAsync(new AuditQueryDto { Limit = 2 })).Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_ReturnsEveryCategoryForAdministrators()
    {
        await _service.RecordAsync(New(action: "user.role.grant", category: AuditCategory.User));
        await _service.RecordAsync(New(action: "post.delete", category: AuditCategory.Content));
        await _service.RecordAsync(New(action: "login", category: AuditCategory.Security));

        (await _service.GetAsync(new AuditQueryDto())).Should().HaveCount(3);
    }

    // ── GetModerationAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetModeration_ReturnsOnlyTheContentSlice()
    {
        await _service.RecordAsync(New(action: "user.role.grant", category: AuditCategory.User));
        await _service.RecordAsync(New(action: "login", category: AuditCategory.Security));
        await _service.RecordAsync(New(action: "system.backup", category: AuditCategory.System));
        await _service.RecordAsync(New(action: "post.delete", category: AuditCategory.Content));
        await _service.RecordAsync(New(action: "report.resolve", category: AuditCategory.Moderation));

        var result = await _service.GetModerationAsync(new AuditQueryDto());

        result.Select(e => e.Action).Should().BeEquivalentTo(["post.delete", "report.resolve"]);
    }

    [Fact]
    public async Task GetModeration_IgnoresAFilterThatWouldWidenTheScope()
    {
        // The "user" row is administrator-only; the "post" row is in scope but is
        // a different target type. A moderator asking for targetType=user must
        // therefore get nothing, rather than the caller widening its own slice.
        await _service.RecordAsync(New(action: "user.role.grant", category: AuditCategory.User, targetType: "user"));
        await _service.RecordAsync(New(action: "post.delete", category: AuditCategory.Content, targetType: "post"));
        var result = await _service.GetModerationAsync(new AuditQueryDto { TargetType = "user" });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetModeration_StillAppliesTheOtherFilters()
    {
        var mine = Guid.NewGuid();
        await _service.RecordAsync(New(action: "theirs", actorId: Guid.NewGuid(), category: AuditCategory.Content));
        await _service.RecordAsync(New(action: "mine", actorId: mine, category: AuditCategory.Content));

        var result = await _service.GetModerationAsync(new AuditQueryDto { ActorId = mine });

        result.Should().ContainSingle();
        result[0].Action.Should().Be("mine");
    }

    // ── Query limit clamping ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(50, 50)]
    [InlineData(10_000, AuditQueryDto.MaxLimit)]
    public void Query_ClampsTheLimitToASaneRange(int requested, int expected)
    {
        new AuditQueryDto { Limit = requested }.Limit.Should().Be(expected);
    }

    [Fact]
    public void Query_DefaultsToAReasonablePageSize()
    {
        new AuditQueryDto().Limit.Should().Be(100);
    }

    private static AuditRecordDto New(
        string action = "user.role.grant",
        Guid? actorId = null,
        string targetType = "user",
        AuditCategory category = AuditCategory.User) => new()
    {
        ActorId = actorId ?? Guid.NewGuid(),
        Action = action,
        TargetType = targetType,
        TargetId = "target-1",
        Category = category
    };

    /// <summary>
    /// Writes an entry straight through the context with an explicit timestamp.
    /// AuditService always stamps DateTime.UtcNow itself, so ordering and window
    /// tests need to bypass it to get a deterministic spread of times.
    /// </summary>
    private async Task SeedAsync(string action, DateTime createdAt)
    {
        _context.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorId = Guid.NewGuid(),
            Action = action,
            TargetType = "user",
            TargetId = "target-1",
            Category = AuditCategory.User,
            CreatedAt = createdAt
        });

        await _context.SaveChangesAsync();
    }
}
