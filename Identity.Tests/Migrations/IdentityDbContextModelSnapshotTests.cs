using FluentAssertions;
using IdentityDAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;
using Xunit;

namespace Identity.Tests.Migrations;

public class IdentityDbContextModelSnapshotTests
{
    private static readonly Assembly _dalAssembly =
        typeof(IdentityDbContext).Assembly;

    private static readonly Type? _snapshotType =
        _dalAssembly.GetType("IdentityDAL.Migrations.IdentityDbContextModelSnapshot");

    [Fact]
    public void SnapshotType_ExistsInAssembly()
    {
        _snapshotType.Should().NotBeNull(
            "IdentityDbContextModelSnapshot should exist in IdentityDAL assembly");
    }

    [Fact]
    public void SnapshotType_InheritsFromModelSnapshot()
    {
        _snapshotType.Should().NotBeNull();
        _snapshotType!.IsAssignableTo(typeof(ModelSnapshot)).Should().BeTrue();
    }

    [Fact]
    public void SnapshotType_IsInCorrectNamespace()
    {
        _snapshotType.Should().NotBeNull();
        _snapshotType!.Namespace.Should().Be("IdentityDAL.Migrations");
    }

    [Fact]
    public void SnapshotType_IsNotAbstract()
    {
        _snapshotType.Should().NotBeNull();
        _snapshotType!.IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void SnapshotType_HasBuildModelMethod()
    {
        _snapshotType.Should().NotBeNull();

        var method = _snapshotType!.GetMethod(
            "BuildModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull();
    }

    [Fact]
    public void SnapshotType_HasDbContextAttribute_PointingToIdentityDbContext()
    {
        _snapshotType.Should().NotBeNull();

        var attr = _snapshotType!.GetCustomAttribute<DbContextAttribute>();

        attr.Should().NotBeNull();
        attr!.ContextType.Should().Be(typeof(IdentityDbContext));
    }

    [Fact]
    public void SnapshotType_CanBeInstantiatedViaReflection()
    {
        _snapshotType.Should().NotBeNull();

        var instance = Activator.CreateInstance(_snapshotType!);

        instance.Should().NotBeNull();
        instance.Should().BeAssignableTo<ModelSnapshot>();
    }

    // ── BuildModel execution ──────────────────────────────────────────────────

    [Fact]
    public void BuildModel_ExecutesWithoutThrowing()
    {
        _snapshotType.Should().NotBeNull();

        var instance = Activator.CreateInstance(_snapshotType!);
        var method   = _snapshotType!.GetMethod(
            "BuildModel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull();

        var modelBuilder = new ModelBuilder();
        var act = () => method!.Invoke(instance, new object[] { modelBuilder });
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildModel_RegistersApplicationUserEntity()
    {
        _snapshotType.Should().NotBeNull();

        var instance     = Activator.CreateInstance(_snapshotType!);
        var method       = _snapshotType!.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var modelBuilder = new ModelBuilder();
        method!.Invoke(instance, new object[] { modelBuilder });

        var userType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType.Should().NotBeNull();
    }

    [Fact]
    public void BuildModel_RegistersRefreshTokenEntity()
    {
        _snapshotType.Should().NotBeNull();

        var instance     = Activator.CreateInstance(_snapshotType!);
        var method       = _snapshotType!.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var modelBuilder = new ModelBuilder();
        method!.Invoke(instance, new object[] { modelBuilder });

        var tokenType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.RefreshToken");
        tokenType.Should().NotBeNull();
    }

    [Fact]
    public void BuildModel_ApplicationUserHasAvatarUrlProperty()
    {
        _snapshotType.Should().NotBeNull();

        var instance     = Activator.CreateInstance(_snapshotType!);
        var method       = _snapshotType!.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var modelBuilder = new ModelBuilder();
        method!.Invoke(instance, new object[] { modelBuilder });

        var userType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType!.FindProperty("AvatarUrl").Should().NotBeNull();
    }

    [Fact]
    public void BuildModel_ApplicationUserHasFullNameAndSpecializationProperties()
    {
        _snapshotType.Should().NotBeNull();

        var instance     = Activator.CreateInstance(_snapshotType!);
        var method       = _snapshotType!.GetMethod("BuildModel", BindingFlags.NonPublic | BindingFlags.Instance);
        var modelBuilder = new ModelBuilder();
        method!.Invoke(instance, new object[] { modelBuilder });

        var userType = modelBuilder.Model.FindEntityType("IdentityServiceDomain.Entities.ApplicationUser");
        userType!.FindProperty("FullName").Should().NotBeNull();
        userType.FindProperty("Specialization").Should().NotBeNull();
    }
}
