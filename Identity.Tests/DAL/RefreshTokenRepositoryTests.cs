using FluentAssertions;
using IdentityDAL.Data;
using IdentityDAL.Repositories;
using IdentityServiceDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Identity.Tests.DAL;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly IdentityDbContext      _context;
    private readonly RefreshTokenRepository _repo;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new IdentityDbContext(options);
        _repo    = new RefreshTokenRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private ApplicationUser AddUser(string email = "user@test.com")
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private RefreshToken AddToken(ApplicationUser user, string tokenValue = "tok", bool isRevoked = false)
    {
        var token = new RefreshToken
        {
            Token     = tokenValue,
            Expires   = DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            CreatedAt = DateTime.UtcNow,
            UserId    = user.Id
        };
        _context.RefreshTokens.Add(token);
        _context.SaveChanges();
        return token;
    }

    // ── GetByTokenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTokenAsync_WhenTokenDoesNotExist_ReturnsNull()
    {
        var result = await _repo.GetByTokenAsync("ghost-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WhenTokenExists_ReturnsToken()
    {
        var user  = AddUser();
        var token = AddToken(user, "my-unique-token");

        var result = await _repo.GetByTokenAsync("my-unique-token");

        result.Should().NotBeNull();
        result!.Token.Should().Be("my-unique-token");
    }

    [Fact]
    public async Task GetByTokenAsync_WhenTokenExists_IncludesUser()
    {
        var user  = AddUser("alice@test.com");
        var token = AddToken(user, "tok-with-user");

        var result = await _repo.GetByTokenAsync("tok-with-user");

        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetByTokenAsync_WithDifferentToken_ReturnsNull()
    {
        var user = AddUser();
        AddToken(user, "token-A");

        var result = await _repo.GetByTokenAsync("token-B");

        result.Should().BeNull();
    }

    // ── GetUserTokensAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserTokensAsync_WhenNoTokensForUser_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();

        var result = await _repo.GetUserTokensAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTokensAsync_ReturnsOnlyTokensForSpecifiedUser()
    {
        var user1 = AddUser("user1@test.com");
        var user2 = AddUser("user2@test.com");
        AddToken(user1, "tok-u1");
        AddToken(user2, "tok-u2");

        var result = (await _repo.GetUserTokensAsync(user1.Id)).ToList();

        result.Should().HaveCount(1);
        result[0].Token.Should().Be("tok-u1");
    }

    [Fact]
    public async Task GetUserTokensAsync_ReturnsAllTokensForUser()
    {
        var user = AddUser();
        AddToken(user, "tok-1");
        AddToken(user, "tok-2");
        AddToken(user, "tok-3");

        var result = await _repo.GetUserTokensAsync(user.Id);

        result.Should().HaveCount(3);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsTokenToDatabase()
    {
        var user = AddUser();
        var token = new RefreshToken
        {
            Token     = "new-token",
            Expires   = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId    = user.Id
        };

        await _repo.AddAsync(token);

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "new-token");
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_AssignsId()
    {
        var user = AddUser();
        var token = new RefreshToken
        {
            Token     = "id-token",
            Expires   = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId    = user.Id
        };

        await _repo.AddAsync(token);

        token.Id.Should().BeGreaterThan(0);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChangesToDatabase()
    {
        var user  = AddUser();
        var token = AddToken(user, "update-tok");

        token.IsRevoked = true;
        await _repo.UpdateAsync(token);

        var updated = await _context.RefreshTokens.FindAsync(token.Id);
        updated!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_CanMarkTokenAsUsed()
    {
        var user  = AddUser();
        var token = AddToken(user, "use-tok");

        token.IsUsed = true;
        await _repo.UpdateAsync(token);

        var updated = await _context.RefreshTokens.FindAsync(token.Id);
        updated!.IsUsed.Should().BeTrue();
    }
}
