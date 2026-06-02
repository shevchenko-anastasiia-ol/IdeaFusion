using System.Data;
using ContentDAL.Connection;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Minio;
using Moq;
using Xunit;

namespace Content.Tests.UOW;

public class UnitOfWorkTests
{
    private readonly Mock<IConnectionFactory> _factoryMock = new();
    private readonly Mock<IDbConnection>      _connMock    = new();
    private readonly Mock<IMinioClient>        _minioMock   = new();

    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Minio:BucketName"] = "test-bucket",
            ["Minio:Endpoint"]   = "localhost:9000"
        })
        .Build();

    public UnitOfWorkTests()
    {
        _factoryMock.Setup(f => f.CreateConnection()).Returns(_connMock.Object);
    }

    private UnitOfWork Create() => new(_factoryMock.Object, _minioMock.Object, _config);

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_CallsCreateConnection()
    {
        Create();
        _factoryMock.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Fact]
    public void Constructor_InitializesPostRepository()
    {
        Create().PostRepository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCommentRepository()
    {
        Create().CommentRepository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesLikeRepository()
    {
        Create().LikeRepository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesPostViewRepository()
    {
        Create().PostViewRepository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesSavedPostRepository()
    {
        Create().SavedPostRepository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesTagRepository()
    {
        Create().TagRepository.Should().NotBeNull();
    }

    // ── BeginTransactionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task BeginTransactionAsync_WhenConnectionClosed_OpensConnectionAndBeginsTransaction()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Closed);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        await Create().BeginTransactionAsync();

        _connMock.Verify(c => c.Open(), Times.Once);
        _connMock.Verify(c => c.BeginTransaction(), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenConnectionAlreadyOpen_DoesNotOpenAgain()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        await Create().BeginTransactionAsync();

        _connMock.Verify(c => c.Open(), Times.Never);
    }

    [Fact]
    public async Task BeginTransactionAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();

        Func<Task> act = () => uow.BeginTransactionAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── CommitAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CommitAsync_CommitsTransaction()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();
        await uow.CommitAsync();

        txMock.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_DisposesTransaction()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();
        await uow.CommitAsync();

        txMock.Verify(t => t.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WhenCommitThrows_RollsBackAndRethrows()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);
        txMock.Setup(t => t.Commit()).Throws(new InvalidOperationException("commit failed"));

        var uow = Create();
        await uow.BeginTransactionAsync();

        Func<Task> act = () => uow.CommitAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
        txMock.Verify(t => t.Rollback(), Times.Once);
    }

    // ── RollbackAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RollbackAsync_WhenTransactionExists_RollsBack()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();
        await uow.RollbackAsync();

        txMock.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_WhenNoTransaction_DoesNotThrow()
    {
        Func<Task> act = () => Create().RollbackAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackAsync_DisposesTransaction()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();
        await uow.RollbackAsync();

        txMock.Verify(t => t.Dispose(), Times.Once);
    }

    // ── DisposeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_WhenConnectionOpen_ClosesAndDisposesConnection()
    {
        _connMock.SetupGet(c => c.State).Returns(ConnectionState.Open);

        await Create().DisposeAsync();

        _connMock.Verify(c => c.Close(), Times.Once);
        _connMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenConnectionClosed_DoesNotCallClose()
    {
        _connMock.SetupGet(c => c.State).Returns(ConnectionState.Closed);

        await Create().DisposeAsync();

        _connMock.Verify(c => c.Close(), Times.Never);
        _connMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotDisposeConnectionTwice()
    {
        _connMock.SetupGet(c => c.State).Returns(ConnectionState.Closed);

        var uow = Create();
        await uow.DisposeAsync();
        await uow.DisposeAsync();

        _connMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenTransactionPending_RollsBackFirst()
    {
        var txMock = new Mock<IDbTransaction>();
        _connMock.SetupGet(c => c.State).Returns(ConnectionState.Open);
        _connMock.Setup(c => c.BeginTransaction()).Returns(txMock.Object);

        var uow = Create();
        await uow.BeginTransactionAsync();
        await uow.DisposeAsync();

        txMock.Verify(t => t.Rollback(), Times.Once);
    }
}
