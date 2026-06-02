using FluentAssertions;
using IdentityDAL.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Identity.Tests.Migrations;

public class AddAvatarUrlTests
{
    private readonly AddAvatarUrl                      _migration = new();
    private readonly IReadOnlyList<MigrationOperation> _upOps;
    private readonly IReadOnlyList<MigrationOperation> _downOps;

    public AddAvatarUrlTests()
    {
        _upOps   = _migration.UpOperations;
        _downOps = _migration.DownOperations;
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Up_AddsAvatarUrlColumnToAspNetUsers()
    {
        _upOps.OfType<AddColumnOperation>()
            .Should().ContainSingle(op => op.Table == "AspNetUsers" && op.Name == "AvatarUrl");
    }

    [Fact]
    public void Up_AvatarUrlColumn_IsNullable()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "AvatarUrl");

        op.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Up_AvatarUrlColumn_IsTextType()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "AvatarUrl");

        op.ColumnType.Should().Be("text");
    }

    [Fact]
    public void Up_ContainsExactlyOneOperation()
    {
        _upOps.Should().HaveCount(1);
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Down_DropsAvatarUrlColumnFromAspNetUsers()
    {
        _downOps.OfType<DropColumnOperation>()
            .Should().ContainSingle(op => op.Table == "AspNetUsers" && op.Name == "AvatarUrl");
    }

    [Fact]
    public void Down_ContainsExactlyOneOperation()
    {
        _downOps.Should().HaveCount(1);
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
        typeof(AddAvatarUrl).Namespace.Should().Be("IdentityDAL.Migrations");
    }

    // ── Designer (BuildTargetModel) ───────────────────────────────────────────

    [Fact]
    public void BuildTargetModel_ExecutesWithoutThrowing()
    {
        var method = typeof(AddAvatarUrl).GetMethod(
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
        var method = typeof(AddAvatarUrl).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var modelBuilder = new ModelBuilder();
        method!.Invoke(_migration, new object[] { modelBuilder });

        var userType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType.Should().NotBeNull();
    }
}
