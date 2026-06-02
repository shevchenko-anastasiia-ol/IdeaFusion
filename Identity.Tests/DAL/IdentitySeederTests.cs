using FluentAssertions;
using IdentityDAL.Data;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace Identity.Tests.DAL;

public class IdentitySeederTests
{
    // ── Reflection / metadata ─────────────────────────────────────────────────

    [Fact]
    public void SeedAsync_IsStaticMethod()
    {
        var method = typeof(IdentitySeeder).GetMethod(nameof(IdentitySeeder.SeedAsync));

        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    [Fact]
    public void SeedAsync_IsPublic()
    {
        var method = typeof(IdentitySeeder).GetMethod(nameof(IdentitySeeder.SeedAsync));

        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void SeedAsync_ReturnsTask()
    {
        var method = typeof(IdentitySeeder).GetMethod(nameof(IdentitySeeder.SeedAsync));

        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void SeedAsync_AcceptsIServiceProvider()
    {
        var method     = typeof(IdentitySeeder).GetMethod(nameof(IdentitySeeder.SeedAsync));
        var parameters = method!.GetParameters();

        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(IServiceProvider));
    }

    [Fact]
    public void IdentitySeeder_IsNotAbstract()
    {
        typeof(IdentitySeeder).IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void IdentitySeeder_IsInCorrectNamespace()
    {
        typeof(IdentitySeeder).Namespace.Should().Be("IdentityDAL.Data");
    }

    // ── No-op migrator so InMemory / fake-Npgsql context doesn't require a DB ─

    private sealed class NoOpMigrator : IMigrator
    {
        public void Migrate(string? targetMigration = null) { }

        public Task MigrateAsync(
            string? targetMigration = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public string GenerateScript(
            string? fromMigration = null,
            string? toMigration = null,
            MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) => string.Empty;

        public bool HasPendingModelChanges() => false;
    }

    // ── Shared mock builders ──────────────────────────────────────────────────

    private static (Mock<UserManager<ApplicationUser>> userMgr, Mock<RoleManager<ApplicationRole>> roleMgr, ILogger logger)
        BuildManagerMocks()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userMgr = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleMgr = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!);

        var logger = Mock.Of<ILogger>();

        return (userMgr, roleMgr, logger);
    }

    private (
        Mock<IServiceProvider> outerProvider,
        Mock<UserManager<ApplicationUser>> userMgr,
        Mock<RoleManager<ApplicationRole>> roleMgr)
        BuildServiceMocks()
    {
        var ctxBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        ctxBuilder.UseNpgsql("Host=localhost;Port=5432;Database=seed_test_unused");
        ctxBuilder.ReplaceService<IMigrator, NoOpMigrator>();
        var ctxOptions = ctxBuilder.Options;

        var ctx = new IdentityDbContext(ctxOptions);

        var (userMgr, roleMgr, _) = BuildManagerMocks();

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider.Setup(p => p.GetService(typeof(ILoggerFactory))).Returns(loggerFactory.Object);
        scopedProvider.Setup(p => p.GetService(typeof(IdentityDbContext))).Returns(ctx);
        scopedProvider.Setup(p => p.GetService(typeof(UserManager<ApplicationUser>))).Returns(userMgr.Object);
        scopedProvider.Setup(p => p.GetService(typeof(RoleManager<ApplicationRole>))).Returns(roleMgr.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var outerProvider = new Mock<IServiceProvider>();
        outerProvider
            .Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactory.Object);

        return (outerProvider, userMgr, roleMgr);
    }

    // ── SeedRolesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedRolesAsync_CreatesRoles_WhenNoneExist()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        roleMgr.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedRolesAsync(roleMgr.Object, logger);

        roleMgr.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SeedRolesAsync_SkipsCreation_WhenRolesAlreadyExist()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        await IdentitySeeder.SeedRolesAsync(roleMgr.Object, logger);

        roleMgr.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task SeedRolesAsync_CreatesAdminAndUserRoles()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();
        var createdRoles = new List<string>();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        roleMgr.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>()))
               .Callback<ApplicationRole>(r => createdRoles.Add(r.Name!))
               .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedRolesAsync(roleMgr.Object, logger);

        createdRoles.Should().Contain("Admin").And.Contain("User");
    }

    // ── SeedTestUsersAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SeedTestUsersAsync_CreatesThreeUsers_WhenNoneExist()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
               .ReturnsAsync((ApplicationUser?)null);
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedTestUsersAsync(userMgr.Object, logger);

        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SeedTestUsersAsync_SkipsUsers_WhenTheyAlreadyExist()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        var existing = new ApplicationUser { Email = "exists@example.com" };
        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existing);

        await IdentitySeeder.SeedTestUsersAsync(userMgr.Object, logger);

        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedTestUsersAsync_LogsWarning_WhenCreationFails()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
               .ReturnsAsync((ApplicationUser?)null);

        var failed = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(failed);

        var act = async () => await IdentitySeeder.SeedTestUsersAsync(userMgr.Object, logger);

        await act.Should().NotThrowAsync();
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    // ── SeedAdminAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAdminAsync_CreatesAdmin_WhenNotExists()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        userMgr.Setup(u => u.FindByEmailAsync("admin@admin.com"))
               .ReturnsAsync((ApplicationUser?)null);
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
               .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userMgr.Object, logger);

        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Admin@1234"), Times.Once);
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task SeedAdminAsync_SkipsCreation_WhenAdminAlreadyExists()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        var existing = new ApplicationUser { Email = "admin@admin.com" };
        userMgr.Setup(u => u.FindByEmailAsync("admin@admin.com")).ReturnsAsync(existing);

        await IdentitySeeder.SeedAdminAsync(userMgr.Object, logger);

        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdminAsync_LogsWarning_WhenCreationFails()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();

        userMgr.Setup(u => u.FindByEmailAsync("admin@admin.com"))
               .ReturnsAsync((ApplicationUser?)null);

        var failed = IdentityResult.Failed(new IdentityError { Description = "Bad password" });
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(failed);

        var act = async () => await IdentitySeeder.SeedAdminAsync(userMgr.Object, logger);

        await act.Should().NotThrowAsync();
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    // ── CreateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_AddsUserToRole_WhenCreationSucceeds()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();
        var user = new ApplicationUser { Email = "test@test.com" };

        userMgr.Setup(u => u.CreateAsync(user, "pass")).ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(u => u.AddToRoleAsync(user, "User")).ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.CreateUserAsync(userMgr.Object, user, "pass", "User", logger);

        userMgr.Verify(u => u.AddToRoleAsync(user, "User"), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_DoesNotAddToRole_WhenCreationFails()
    {
        var (userMgr, roleMgr, logger) = BuildManagerMocks();
        var user = new ApplicationUser { Email = "test@test.com" };

        var failed = IdentityResult.Failed(new IdentityError { Description = "Weak password" });
        userMgr.Setup(u => u.CreateAsync(user, "pass")).ReturnsAsync(failed);

        var act = async () => await IdentitySeeder.CreateUserAsync(userMgr.Object, user, "pass", "User", logger);

        await act.Should().NotThrowAsync();
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    // ── SeedAsync integration (full flow via mocked services) ─────────────────

    [Fact]
    public async Task SeedAsync_CreatesRolesAndUsersAndAdmin_WhenNoneExist()
    {
        var (provider, userMgr, roleMgr) = BuildServiceMocks();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        roleMgr.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
               .ReturnsAsync((ApplicationUser?)null);
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAsync(provider.Object);

        roleMgr.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Exactly(2));
        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(4));
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(4));
    }

    [Fact]
    public async Task SeedAsync_SkipsCreation_WhenRolesAndUsersAlreadyExist()
    {
        var (provider, userMgr, roleMgr) = BuildServiceMocks();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var existing = new ApplicationUser { Email = "exists@example.com" };
        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existing);

        await IdentitySeeder.SeedAsync(provider.Object);

        roleMgr.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_LogsWarning_WhenUserAndAdminCreationFails()
    {
        var (provider, userMgr, roleMgr) = BuildServiceMocks();

        roleMgr.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        userMgr.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
               .ReturnsAsync((ApplicationUser?)null);

        var failed = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });
        userMgr.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(failed);

        var act = async () => await IdentitySeeder.SeedAsync(provider.Object);
        await act.Should().NotThrowAsync();

        userMgr.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(4));
        userMgr.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }
}
