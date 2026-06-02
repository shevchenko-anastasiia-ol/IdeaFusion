using ContentDAL.Connection;
using ContentDAL.Data;
using FluentAssertions;
using Xunit;

namespace Content.Tests.DAL;

public class ContentDbContextFactoryTests
{
    private readonly ContentDbContextFactory _factory = new();

    // ── CreateDbContext ───────────────────────────────────────────────────────

    [Fact]
    public void CreateDbContext_ReturnsNonNullContext()
    {
        using var ctx = _factory.CreateDbContext(Array.Empty<string>());

        ctx.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_ReturnsContentDbContext()
    {
        using var ctx = _factory.CreateDbContext(Array.Empty<string>());

        ctx.Should().BeOfType<ContentDbContext>();
    }

    [Fact]
    public void CreateDbContext_UsesNpgsqlProvider()
    {
        using var ctx = _factory.CreateDbContext(Array.Empty<string>());

        ctx.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void CreateDbContext_WithArgs_DoesNotThrow()
    {
        Action act = () =>
        {
            using var ctx = _factory.CreateDbContext(new[] { "arg1", "arg2" });
        };

        act.Should().NotThrow();
    }
}
