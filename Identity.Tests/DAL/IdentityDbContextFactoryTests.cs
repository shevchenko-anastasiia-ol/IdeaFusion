using FluentAssertions;
using IdentityDAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Identity.Tests.DAL;

public class IdentityDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsNonNullContext()
    {
        var factory = new IdentityDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_ReturnsIdentityDbContext()
    {
        var factory = new IdentityDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.Should().BeOfType<IdentityDbContext>();
    }

    [Fact]
    public void CreateDbContext_ContextHasAuditLogEntriesDbSet()
    {
        var factory = new IdentityDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.AuditLogEntries.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_UsesNpgsqlProvider()
    {
        var factory = new IdentityDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void CreateDbContext_WithEmptyArgs_DoesNotThrow()
    {
        var factory = new IdentityDbContextFactory();

        var act = () =>
        {
            using var ctx = factory.CreateDbContext(Array.Empty<string>());
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateDbContext_CalledTwice_ReturnsTwoDistinctInstances()
    {
        var factory = new IdentityDbContextFactory();
        using var ctx1 = factory.CreateDbContext(Array.Empty<string>());
        using var ctx2 = factory.CreateDbContext(Array.Empty<string>());

        ctx1.Should().NotBeSameAs(ctx2);
    }

    [Fact]
    public void Factory_ImplementsIDesignTimeDbContextFactory()
    {
        typeof(IdentityDbContextFactory)
            .Should().Implement<Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<IdentityDbContext>>();
    }
}
