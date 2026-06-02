using Collaboration.Infrastructure;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure;

public class MongoUnitOfWorkTests
{
    private readonly Mock<IMongoDatabase>      _dbMock      = new();
    private readonly Mock<IMongoClient>        _clientMock  = new();
    private readonly Mock<IClientSessionHandle> _sessionMock = new();

    public MongoUnitOfWorkTests()
    {
        _dbMock.Setup(d => d.Client).Returns(_clientMock.Object);

        _clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sessionMock.Object);

        _sessionMock
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sessionMock
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ── Session property ─────────────────────────────────────────────────────

    [Fact]
    public void Session_BeforeStartTransactionAsync_ThrowsInvalidOperationException()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        uow.Invoking(u => { _ = u.Session; })
           .Should().Throw<InvalidOperationException>()
           .WithMessage("*Session has not been started*");
    }

    [Fact]
    public async Task Session_AfterStartTransactionAsync_ReturnsSession()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        uow.Session.Should().BeSameAs(_sessionMock.Object);
    }

    // ── StartTransactionAsync ────────────────────────────────────────────────

    [Fact]
    public async Task StartTransactionAsync_StartsClientSessionAndTransaction()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        await uow.StartTransactionAsync();

        _clientMock.Verify(
            c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _sessionMock.Verify(s => s.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
    }

    // ── CommitAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CommitAsync_WithActiveSession_CommitsTransaction()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        await uow.CommitAsync();

        _sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WithActiveSession_DisposesSession()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        await uow.CommitAsync();

        _sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WithActiveSession_ClearsSessionSoSubsequentAccessThrows()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();
        await uow.CommitAsync();

        uow.Invoking(u => { _ = u.Session; })
           .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CommitAsync_WithNoSession_ThrowsInvalidOperationException()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        await uow.Invoking(u => u.CommitAsync())
                 .Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*No active session*");
    }

    // ── AbortAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AbortAsync_WithActiveSession_AbortsTransaction()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        await uow.AbortAsync();

        _sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AbortAsync_WithActiveSession_DisposesSession()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        await uow.AbortAsync();

        _sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task AbortAsync_WithNoSession_DoesNotThrow()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        await uow.Invoking(u => u.AbortAsync()).Should().NotThrowAsync();
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_WithActiveSession_DisposesSession()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);
        await uow.StartTransactionAsync();

        uow.Dispose();

        _sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WithNoSession_DoesNotThrow()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        uow.Invoking(u => u.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var uow = new MongoUnitOfWork(_dbMock.Object);

        uow.Dispose();
        uow.Invoking(u => u.Dispose()).Should().NotThrow();
    }
}