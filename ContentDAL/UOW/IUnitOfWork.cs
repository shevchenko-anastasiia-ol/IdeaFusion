using ContentDAL.Repository.Interfaces;

namespace ContentDAL.UOW;

public interface IUnitOfWork : IAsyncDisposable
{
    IPostRepository PostRepository { get; }
    ICommentRepository CommentRepository { get; }
    ILikeRepository LikeRepository { get; }
    IPostViewRepository PostViewRepository { get; }
    ISavedPostRepository SavedPostRepository { get; }
    ITagRepository TagRepository { get; }
 
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}