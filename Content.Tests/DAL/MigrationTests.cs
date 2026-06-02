using ContentDAL.Data;
using ContentDAL.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Content.Tests.DAL;

public class InitialCreateMigrationTests
{
    private static List<MigrationOperation> RunUp()
    {
        var migration = new InitialCreate();
        var builder   = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(InitialCreate)
            .GetMethod("Up", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(migration, new object[] { builder });
        return builder.Operations;
    }

    private static List<MigrationOperation> RunDown()
    {
        var migration = new InitialCreate();
        var builder   = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(InitialCreate)
            .GetMethod("Down", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(migration, new object[] { builder });
        return builder.Operations;
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Up_Creates10Tables()
    {
        RunUp().OfType<CreateTableOperation>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData("collaborationsnapshots")]
    [InlineData("postauthors")]
    [InlineData("posts")]
    [InlineData("tags")]
    [InlineData("posttags")]
    [InlineData("comments")]
    [InlineData("likes")]
    [InlineData("postmedia")]
    [InlineData("postviews")]
    [InlineData("savedposts")]
    public void Up_CreatesExpectedTable(string tableName)
    {
        RunUp().OfType<CreateTableOperation>()
               .Should().Contain(o => o.Name == tableName);
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Down_Drops10Tables()
    {
        RunDown().OfType<DropTableOperation>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData("collaborationsnapshots")]
    [InlineData("postauthors")]
    [InlineData("posts")]
    [InlineData("tags")]
    [InlineData("posttags")]
    [InlineData("comments")]
    [InlineData("likes")]
    [InlineData("postmedia")]
    [InlineData("postviews")]
    [InlineData("savedposts")]
    public void Down_DropsExpectedTable(string tableName)
    {
        RunDown().OfType<DropTableOperation>()
                 .Should().Contain(o => o.Name == tableName);
    }
}

public class AddCollaborationSnapshotExternalIdMigrationTests
{
    private static List<MigrationOperation> RunUp()
    {
        var migration = new AddCollaborationSnapshotExternalId();
        var builder   = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddCollaborationSnapshotExternalId)
            .GetMethod("Up", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(migration, new object[] { builder });
        return builder.Operations;
    }

    private static List<MigrationOperation> RunDown()
    {
        var migration = new AddCollaborationSnapshotExternalId();
        var builder   = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddCollaborationSnapshotExternalId)
            .GetMethod("Down", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(migration, new object[] { builder });
        return builder.Operations;
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Up_AddsExactlyOneColumn()
    {
        RunUp().OfType<AddColumnOperation>().Should().HaveCount(1);
    }

    [Fact]
    public void Up_AddsExternalIdColumn_ToCollaborationSnapshots()
    {
        RunUp().OfType<AddColumnOperation>()
               .Should().Contain(o =>
                   o.Name  == "externalid" &&
                   o.Table == "collaborationsnapshots");
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Down_DropsExactlyOneColumn()
    {
        RunDown().OfType<DropColumnOperation>().Should().HaveCount(1);
    }

    [Fact]
    public void Down_DropsExternalIdColumn_FromCollaborationSnapshots()
    {
        RunDown().OfType<DropColumnOperation>()
                 .Should().Contain(o =>
                     o.Name  == "externalid" &&
                     o.Table == "collaborationsnapshots");
    }
}

public class ContentDbContextModelSnapshotTests
{
    // ContentDbContextModelSnapshot is internal — access via assembly reflection
    private static readonly Type SnapshotType =
        typeof(ContentDbContext).Assembly
            .GetType("ContentDAL.Migrations.ContentDbContextModelSnapshot")!;

    [Fact]
    public void ModelSnapshot_TypeExists_InContentDALAssembly()
    {
        SnapshotType.Should().NotBeNull();
    }

    [Fact]
    public void ModelSnapshot_HasDbContextAttribute_ForContentDbContext()
    {
        var attr = SnapshotType.GetCustomAttribute<DbContextAttribute>();

        attr.Should().NotBeNull();
        attr!.ContextType.Should().Be<ContentDbContext>();
    }

    [Fact]
    public void ModelSnapshot_InheritsFromModelSnapshot()
    {
        typeof(ModelSnapshot).IsAssignableFrom(SnapshotType).Should().BeTrue();
    }

    [Fact]
    public void BuildModel_ExecutesWithoutThrowing()
    {
        var instance = Activator.CreateInstance(SnapshotType);
        var method   = SnapshotType.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        var mb  = new ModelBuilder();
        var act = () => method!.Invoke(instance, new object[] { mb });
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildModel_RegistersPostEntity()
    {
        var instance = Activator.CreateInstance(SnapshotType);
        var method   = SnapshotType.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var mb       = new ModelBuilder();
        method!.Invoke(instance, new object[] { mb });

        mb.Model.FindEntityType("ContentDomain.Entity.Post").Should().NotBeNull();
    }

    [Fact]
    public void BuildModel_RegistersTagEntity()
    {
        var instance = Activator.CreateInstance(SnapshotType);
        var method   = SnapshotType.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var mb       = new ModelBuilder();
        method!.Invoke(instance, new object[] { mb });

        mb.Model.FindEntityType("ContentDomain.Entity.Tag").Should().NotBeNull();
    }

    [Fact]
    public void BuildModel_RegistersCommentEntity()
    {
        var instance = Activator.CreateInstance(SnapshotType);
        var method   = SnapshotType.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var mb       = new ModelBuilder();
        method!.Invoke(instance, new object[] { mb });

        mb.Model.FindEntityType("ContentDomain.Entity.Comment").Should().NotBeNull();
    }
}

public class InitialCreateBuildTargetModelTests
{
    private static void InvokeBuildTargetModel(object migration, ModelBuilder mb)
    {
        var method = typeof(InitialCreate).GetMethod(
            "BuildTargetModel", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(migration, new object[] { mb });
    }

    [Fact]
    public void BuildTargetModel_ExecutesWithoutThrowing()
    {
        var mb  = new ModelBuilder();
        var act = () => InvokeBuildTargetModel(new InitialCreate(), mb);
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildTargetModel_RegistersPostEntity()
    {
        var mb = new ModelBuilder();
        InvokeBuildTargetModel(new InitialCreate(), mb);
        mb.Model.FindEntityType("ContentDomain.Entity.Post").Should().NotBeNull();
    }

    [Fact]
    public void BuildTargetModel_RegistersCommentEntity()
    {
        var mb = new ModelBuilder();
        InvokeBuildTargetModel(new InitialCreate(), mb);
        mb.Model.FindEntityType("ContentDomain.Entity.Comment").Should().NotBeNull();
    }
}

public class AddCollaborationSnapshotExternalIdBuildTargetModelTests
{
    private static void InvokeBuildTargetModel(object migration, ModelBuilder mb)
    {
        var method = typeof(AddCollaborationSnapshotExternalId).GetMethod(
            "BuildTargetModel", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(migration, new object[] { mb });
    }

    [Fact]
    public void BuildTargetModel_ExecutesWithoutThrowing()
    {
        var mb  = new ModelBuilder();
        var act = () => InvokeBuildTargetModel(new AddCollaborationSnapshotExternalId(), mb);
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildTargetModel_RegistersCollaborationSnapshotEntity()
    {
        var mb = new ModelBuilder();
        InvokeBuildTargetModel(new AddCollaborationSnapshotExternalId(), mb);
        mb.Model.FindEntityType("ContentDomain.Entity.CollaborationSnapshot").Should().NotBeNull();
    }

    [Fact]
    public void BuildTargetModel_RegistersPostTagEntity()
    {
        var mb = new ModelBuilder();
        InvokeBuildTargetModel(new AddCollaborationSnapshotExternalId(), mb);
        mb.Model.FindEntityType("ContentDomain.Entity.PostTag").Should().NotBeNull();
    }
}
