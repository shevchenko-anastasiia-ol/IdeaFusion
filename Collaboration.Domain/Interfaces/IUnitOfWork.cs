using MongoDB.Driver;

namespace Collaboration.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IClientSessionHandle Session { get; }
    Task StartTransactionAsync();
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task AbortAsync(CancellationToken cancellationToken = default);
}