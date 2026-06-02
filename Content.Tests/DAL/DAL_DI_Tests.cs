using ContentDAL;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Xunit;

namespace Content.Tests.DAL;

public class DAL_DI_Tests
{
    private static IServiceCollection Build() =>
        new ServiceCollection().AddDataAccess(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ContentDb"] = "Host=localhost;Port=5432;Database=content;Username=postgres;Password=pass",
                    ["Minio:BucketName"]             = "test-bucket",
                    ["Minio:Endpoint"]               = "localhost:9000"
                })
                .Build());

    // ── Returns same collection ───────────────────────────────────────────────

    [Fact]
    public void AddDataAccess_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var cfg      = new ConfigurationBuilder().Build();

        var result = services.AddDataAccess(cfg);

        result.Should().BeSameAs(services);
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddDataAccess_RegistersContentDbContext()
    {
        var services = Build();

        services.Any(d => d.ServiceType == typeof(ContentDbContext)).Should().BeTrue();
    }

    // ── Connection ────────────────────────────────────────────────────────────

    [Fact]
    public void AddDataAccess_RegistersIConnectionFactory_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IConnectionFactory) &&
            d.Lifetime    == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersIDbConnection_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IDbConnection) &&
            d.Lifetime    == ServiceLifetime.Scoped);
    }

    // ── Repositories ──────────────────────────────────────────────────────────

    [Fact]
    public void AddDataAccess_RegistersIPostRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPostRepository) &&
            d.Lifetime    == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersICommentRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(ICommentRepository) &&
            d.ImplementationType == typeof(CommentRepository) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersILikeRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(ILikeRepository) &&
            d.ImplementationType == typeof(LikeRepository) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersIPostViewRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(IPostViewRepository) &&
            d.ImplementationType == typeof(PostViewRepository) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersISavedPostRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(ISavedPostRepository) &&
            d.ImplementationType == typeof(SavedPostRepository) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDataAccess_RegistersITagRepository_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(ITagRepository) &&
            d.ImplementationType == typeof(TagRepository) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }

    // ── Unit of Work ──────────────────────────────────────────────────────────

    [Fact]
    public void AddDataAccess_RegistersIUnitOfWork_AsScoped()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType       == typeof(IUnitOfWork) &&
            d.ImplementationType == typeof(UnitOfWork) &&
            d.Lifetime           == ServiceLifetime.Scoped);
    }
}
