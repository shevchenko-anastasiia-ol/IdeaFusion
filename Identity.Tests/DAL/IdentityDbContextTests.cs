using FluentAssertions;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Identity.Tests.DAL;

public class IdentityDbContextTests : IDisposable
{
    private readonly IdentityDbContext _context;

    public IdentityDbContextTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void Context_CanBeInstantiated()
    {
        _context.Should().NotBeNull();
    }

    [Fact]
    public void AuditLogEntries_DbSetIsNotNull()
    {
        _context.AuditLogEntries.Should().NotBeNull();
    }

    [Fact]
    public void Context_InheritsFromIdentityDbContext()
    {
        _context.Should().BeAssignableTo<Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<
            ApplicationUser, ApplicationRole, Guid>>();
    }

    [Fact]
    public void Users_DbSetIsNotNull()
    {
        _context.Users.Should().NotBeNull();
    }

    [Fact]
    public void Roles_DbSetIsNotNull()
    {
        _context.Roles.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAuditEntry_CanBeRetrievedById()
    {
        var entry = NewEntry(action: "user.role.grant");
        await _context.AuditLogEntries.AddAsync(entry);
        await _context.SaveChangesAsync();

        var retrieved = await _context.AuditLogEntries.FindAsync(entry.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Action.Should().Be("user.role.grant");
        retrieved.TargetId.Should().Be("target-1");
    }

    [Fact]
    public async Task AddMultipleAuditEntries_AllAreStoredAndQueryable()
    {
        await _context.AuditLogEntries.AddRangeAsync(
            NewEntry(action: "a", createdAt: DateTime.UtcNow.AddMinutes(-1)),
            NewEntry(action: "b", createdAt: DateTime.UtcNow));
        await _context.SaveChangesAsync();

        (await _context.AuditLogEntries.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task AuditEntries_AreQueryableByActor()
    {
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();

        await _context.AuditLogEntries.AddRangeAsync(
            NewEntry(action: "mine", actorId: actor),
            NewEntry(action: "theirs", actorId: other));
        await _context.SaveChangesAsync();

        var mine = await _context.AuditLogEntries.Where(e => e.ActorId == actor).ToListAsync();

        mine.Should().HaveCount(1);
        mine[0].Action.Should().Be("mine");
    }

    [Fact]
    public async Task AuditEntries_AreQueryableByCategory()
    {
        await _context.AuditLogEntries.AddRangeAsync(
            NewEntry(action: "post.delete", category: AuditCategory.Content),
            NewEntry(action: "user.role.grant", category: AuditCategory.User));
        await _context.SaveChangesAsync();

        var content = await _context.AuditLogEntries
            .Where(e => e.Category == AuditCategory.Content)
            .ToListAsync();

        content.Should().HaveCount(1);
        content[0].Action.Should().Be("post.delete");
    }

    [Fact]
    public async Task UpdateAuditEntry_ChangesArePersisted()
    {
        var entry = NewEntry(action: "post.delete");
        await _context.AuditLogEntries.AddAsync(entry);
        await _context.SaveChangesAsync();

        entry.Details = "{\"reason\":\"spam\"}";
        _context.AuditLogEntries.Update(entry);
        await _context.SaveChangesAsync();

        var updated = await _context.AuditLogEntries.FindAsync(entry.Id);
        updated!.Details.Should().Be("{\"reason\":\"spam\"}");
    }

    [Fact]
    public void NewAuditEntry_GetsAnIdAndTimestampByDefault()
    {
        var entry = NewEntry(action: "x");

        entry.Id.Should().NotBe(Guid.Empty);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    private static AuditLogEntry NewEntry(
        string action,
        Guid? actorId = null,
        AuditCategory category = AuditCategory.User,
        DateTime? createdAt = null) => new()
        {
            ActorId = actorId ?? Guid.NewGuid(),
            Action = action,
            TargetType = "user",
            TargetId = "target-1",
            Category = category,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
}
