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
    public void RefreshTokens_DbSetIsNotNull()
    {
        _context.RefreshTokens.Should().NotBeNull();
    }

    [Fact]
    public async Task AddRefreshToken_CanBeRetrievedById()
    {
        var user = new ApplicationUser
        {
            Id    = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "testuser"
        };
        _context.Users.Add(user);

        var token = new RefreshToken
        {
            Token     = "test-token",
            Expires   = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId    = user.Id
        };
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        var retrieved = await _context.RefreshTokens.FindAsync(token.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task AddMultipleRefreshTokens_AllAreStoredAndQueryable()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "u@u.com", UserName = "u" };
        _context.Users.Add(user);

        _context.RefreshTokens.Add(new RefreshToken
            { Token = "t1", Expires = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UserId = user.Id });
        _context.RefreshTokens.Add(new RefreshToken
            { Token = "t2", Expires = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UserId = user.Id });
        await _context.SaveChangesAsync();

        var count = await _context.RefreshTokens.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateRefreshToken_ChangesArePersisted()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "u@u.com", UserName = "u" };
        _context.Users.Add(user);
        var token = new RefreshToken
            { Token = "tok", Expires = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow, UserId = user.Id };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        token.IsRevoked = true;
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();

        var updated = await _context.RefreshTokens.FindAsync(token.Id);
        updated!.IsRevoked.Should().BeTrue();
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
    public async Task RefreshTokens_FilterByUserId_ReturnsCorrectTokens()
    {
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@a.com", UserName = "a" };
        var user2 = new ApplicationUser { Id = Guid.NewGuid(), Email = "b@b.com", UserName = "b" };
        _context.Users.AddRange(user1, user2);
        _context.RefreshTokens.Add(new RefreshToken
            { Token = "u1-tok", Expires = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UserId = user1.Id });
        _context.RefreshTokens.Add(new RefreshToken
            { Token = "u2-tok", Expires = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UserId = user2.Id });
        await _context.SaveChangesAsync();

        var user1Tokens = await _context.RefreshTokens
            .Where(t => t.UserId == user1.Id)
            .ToListAsync();

        user1Tokens.Should().HaveCount(1);
        user1Tokens[0].Token.Should().Be("u1-tok");
    }
}
