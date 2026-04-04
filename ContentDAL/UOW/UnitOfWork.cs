using System.Data;
using ContentDAL.Connection;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;

namespace ContentDAL.UOW;

public class UnitOfWork : IUnitOfWork
{
    private readonly IConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed = false;
    private readonly object _lockObject = new object();
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public IPostRepository PostRepository { get; private set; } = null!;
    public ICommentRepository CommentRepository { get; private set; } = null!;
    public ILikeRepository LikeRepository { get; private set; } = null!;
    public IPostViewRepository PostViewRepository { get; private set; } = null!;
    public ISavedPostRepository SavedPostRepository { get; private set; } = null!;
    public ITagRepository TagRepository { get; private set; } = null!;

    public UnitOfWork(
        IConnectionFactory connectionFactory,
        IMinioClient minioClient,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _minioClient = minioClient;
        _bucketName = configuration["Minio:BucketName"] ?? "content";

        _connection = _connectionFactory.CreateConnection();
        InitializeRepositories();
    }

    private void InitializeRepositories()
    {
        if (_connection == null) throw new InvalidOperationException("Connection is null.");

        PostRepository     = new PostRepository(_connection, _minioClient, _bucketName, _transaction);
        CommentRepository  = new CommentRepository(_connection, _transaction);
        LikeRepository     = new LikeRepository(_connection, _transaction);
        PostViewRepository = new PostViewRepository(_connection, _transaction);
        SavedPostRepository = new SavedPostRepository(_connection, _transaction);
        TagRepository      = new TagRepository(_connection, _transaction);
    }

    private void EnsureTransactionStarted()
    {
        lock (_lockObject)
        {
            if (_transaction == null)
            {
                if (_connection?.State != ConnectionState.Open)
                    _connection?.Open();

                _transaction = _connection!.BeginTransaction();
                InitializeRepositories();
            }
        }
    }

    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Транзакція вже активна.");

        EnsureTransactionStarted();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        lock (_lockObject)
        {
            EnsureTransactionStarted();

            if (_transaction == null)
                throw new InvalidOperationException("Немає активної транзакції.");

            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        lock (_lockObject)
        {
            if (_transaction == null)
                return Task.CompletedTask;

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction != null)
            await RollbackAsync();

        if (_connection != null)
        {
            if (_connection.State != ConnectionState.Closed)
                _connection.Close();

            _connection.Dispose();
            _connection = null;
        }

        _disposed = true;
    }
}