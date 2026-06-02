using FluentAssertions;
using IdentityDAL.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Identity.Tests.Migrations;

public class AddFullNameSpecializationTests
{
    private readonly AddFullNameSpecialization          _migration = new();
    private readonly IReadOnlyList<MigrationOperation> _upOps;
    private readonly IReadOnlyList<MigrationOperation> _downOps;

    public AddFullNameSpecializationTests()
    {
        _upOps   = _migration.UpOperations;
        _downOps = _migration.DownOperations;
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Up_AddsFullNameColumnToAspNetUsers()
    {
        _upOps.OfType<AddColumnOperation>()
            .Should().Contain(op => op.Table == "AspNetUsers" && op.Name == "FullName");
    }

    [Fact]
    public void Up_AddsSpecializationColumnToAspNetUsers()
    {
        _upOps.OfType<AddColumnOperation>()
            .Should().Contain(op => op.Table == "AspNetUsers" && op.Name == "Specialization");
    }

    [Fact]
    public void Up_FullNameColumn_IsNullable()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "FullName");

        op.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Up_SpecializationColumn_IsNullable()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "Specialization");

        op.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Up_FullNameColumn_IsTextType()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "FullName");

        op.ColumnType.Should().Be("text");
    }

    [Fact]
    public void Up_SpecializationColumn_IsTextType()
    {
        var op = _upOps.OfType<AddColumnOperation>()
            .Single(o => o.Name == "Specialization");

        op.ColumnType.Should().Be("text");
    }

    [Fact]
    public void Up_ContainsExactlyTwoOperations()
    {
        _upOps.Should().HaveCount(2);
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Down_DropsFullNameColumn()
    {
        _downOps.OfType<DropColumnOperation>()
            .Should().Contain(op => op.Table == "AspNetUsers" && op.Name == "FullName");
    }

    [Fact]
    public void Down_DropsSpecializationColumn()
    {
        _downOps.OfType<DropColumnOperation>()
            .Should().Contain(op => op.Table == "AspNetUsers" && op.Name == "Specialization");
    }

    [Fact]
    public void Down_ContainsExactlyTwoOperations()
    {
        _downOps.Should().HaveCount(2);
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
        typeof(AddFullNameSpecialization).Namespace.Should().Be("IdentityDAL.Migrations");
    }

    // ── Designer (BuildTargetModel) ───────────────────────────────────────────

    [Fact]
    public void BuildTargetModel_ExecutesWithoutThrowing()
    {
        var method = typeof(AddFullNameSpecialization).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull("Designer.cs must declare BuildTargetModel");

        var modelBuilder = new ModelBuilder();
        var act = () => method!.Invoke(_migration, new object[] { modelBuilder });
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildTargetModel_ConfiguresFullNameAndSpecializationOnApplicationUser()
    {
        var method = typeof(AddFullNameSpecialization).GetMethod(
            "BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var modelBuilder = new ModelBuilder();
        method!.Invoke(_migration, new object[] { modelBuilder });

        var userType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType.Should().NotBeNull();

        var fullNameProp       = userType!.FindProperty("FullName");
        var specializationProp = userType.FindProperty("Specialization");

        fullNameProp.Should().NotBeNull();
        specializationProp.Should().NotBeNull();
    }
}
