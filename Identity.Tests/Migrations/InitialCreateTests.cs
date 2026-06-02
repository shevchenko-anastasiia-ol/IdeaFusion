using FluentAssertions;
using IdentityDAL.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Identity.Tests.Migrations;

public class InitialCreateTests
{
    private readonly InitialCreate            _migration = new();
    private readonly IReadOnlyList<MigrationOperation> _upOps;
    private readonly IReadOnlyList<MigrationOperation> _downOps;

    public InitialCreateTests()
    {
        _upOps   = _migration.UpOperations;
        _downOps = _migration.DownOperations;
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Up_CreatesAspNetRolesTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetRoles");
    }

    [Fact]
    public void Up_CreatesAspNetUsersTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUsers");
    }

    [Fact]
    public void Up_CreatesRefreshTokensTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "RefreshTokens");
    }

    [Fact]
    public void Up_CreatesAspNetRoleClaimsTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetRoleClaims");
    }

    [Fact]
    public void Up_CreatesAspNetUserRolesTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUserRoles");
    }

    [Fact]
    public void Up_CreatesAspNetUserClaimsTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUserClaims");
    }

    [Fact]
    public void Up_CreatesAspNetUserLoginsTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUserLogins");
    }

    [Fact]
    public void Up_CreatesAspNetUserTokensTable()
    {
        _upOps.OfType<CreateTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUserTokens");
    }

    [Fact]
    public void Up_RefreshTokensTable_HasTokenColumn()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.Columns.Should().Contain(c => c.Name == "Token");
    }

    [Fact]
    public void Up_RefreshTokensTable_HasIsRevokedColumn()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.Columns.Should().Contain(c => c.Name == "IsRevoked");
    }

    [Fact]
    public void Up_RefreshTokensTable_HasIsUsedColumn()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.Columns.Should().Contain(c => c.Name == "IsUsed");
    }

    [Fact]
    public void Up_RefreshTokensTable_HasExpiresColumn()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.Columns.Should().Contain(c => c.Name == "Expires");
    }

    [Fact]
    public void Up_RefreshTokensTable_HasUserIdForeignKey()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.ForeignKeys.Should().Contain(fk => fk.PrincipalTable == "AspNetUsers");
    }

    [Fact]
    public void Up_RefreshTokensTable_CascadeDeleteOnUser()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "RefreshTokens");

        table.ForeignKeys.Should()
            .Contain(fk => fk.PrincipalTable == "AspNetUsers" &&
                           fk.OnDelete == ReferentialAction.Cascade);
    }

    [Fact]
    public void Up_AspNetUsers_HasCreatedAtColumn()
    {
        var table = _upOps.OfType<CreateTableOperation>()
            .Single(op => op.Name == "AspNetUsers");

        table.Columns.Should().Contain(c => c.Name == "CreatedAt");
    }

    [Fact]
    public void Up_CreatesIndexesForRefreshTokens()
    {
        _upOps.OfType<CreateIndexOperation>()
            .Should().Contain(op => op.Table == "RefreshTokens");
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Down_DropsRefreshTokensTable()
    {
        _downOps.OfType<DropTableOperation>()
            .Should().Contain(op => op.Name == "RefreshTokens");
    }

    [Fact]
    public void Down_DropsAspNetRolesTable()
    {
        _downOps.OfType<DropTableOperation>()
            .Should().Contain(op => op.Name == "AspNetRoles");
    }

    [Fact]
    public void Down_DropsAspNetUsersTable()
    {
        _downOps.OfType<DropTableOperation>()
            .Should().Contain(op => op.Name == "AspNetUsers");
    }

    [Fact]
    public void Down_DropsAllEightTables()
    {
        _downOps.OfType<DropTableOperation>().Should().HaveCount(8);
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    [Fact]
    public void Migration_InheritsFromMigration()
    {
        _migration.Should().BeAssignableTo<Migration>();
    }

    [Fact]
    public void Migration_IsInCorrectNamespace()
    {
        typeof(InitialCreate).Namespace.Should().Be("IdentityDAL.Migrations");
    }

    // ── Designer (BuildTargetModel) ───────────────────────────────────────────

    [Fact]
    public void BuildTargetModel_ExecutesWithoutThrowing()
    {
        var method = typeof(InitialCreate).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull("Designer.cs must declare BuildTargetModel");

        var modelBuilder = new ModelBuilder();
        var act = () => method!.Invoke(_migration, new object[] { modelBuilder });
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildTargetModel_ConfiguresApplicationUserEntity()
    {
        var method = typeof(InitialCreate).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var modelBuilder = new ModelBuilder();
        method!.Invoke(_migration, new object[] { modelBuilder });

        var model = modelBuilder.Model;
        var userType = model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType.Should().NotBeNull();
    }

    [Fact]
    public void BuildTargetModel_ConfiguresRefreshTokenEntity()
    {
        var method = typeof(InitialCreate).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var modelBuilder = new ModelBuilder();
        method!.Invoke(_migration, new object[] { modelBuilder });

        var model = modelBuilder.Model;
        var tokenType = model.FindEntityType("IdentityServiceDomain.Entities.RefreshToken");
        tokenType.Should().NotBeNull();
    }
}
